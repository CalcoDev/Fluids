using Godot;
using System;

namespace Fluids.Scripts.Water.SaintVenand;

public class Simulation
{
    public readonly record struct SimParams(
        int NumCells,
        int SubstepsMax,
        float Dx,
        float G,
        float ManningN,
        float BedSlope,
        float MinDepth,
        float Cfl
    )
    {
        public SimParams() : this(
                NumCells: 0,
                SubstepsMax: 1,
                Dx: 1f,
                G: 9.81f,
                ManningN: 0.03f,
                BedSlope: 0.0f,
                MinDepth: 1e-4f,
                Cfl: 0.5f
            )
        {
        }
    }

    public enum OutflowMode
    {
        OpenCopy,
        FixedDepth
    }

    // Configuration
    public SimParams Params;

    // TODO(calco): Do we put these in params as well? Technically yes, butehhh
    public bool InflowEnabled { get; set; } = false;
    public float InflowQPerWidth { get; set; } = 0.0f;

    public OutflowMode OutflowModeValue { get; set; } = OutflowMode.OpenCopy;
    public float OutflowFixedDepth { get; set; } = 0.2f;

    public ReadOnlySpan<float> H => _h;
    public ReadOnlySpan<float> Q => _q;

    // Stats
    public float MaxWaveSpeed { get; private set; }
    public float MaxDepth { get; private set; }
    public int LastSubsteps { get; private set; }

    // Internals
    private float[] _h;      // depth
    private float[] _q;      // discharge per unit width (q = h * u)
    private float[] _hNew;
    private float[] _qNew;
    private readonly float[] _fh;     // mass flux at interfaces (length N+1)
    private readonly float[] _fq;     // momentum flux at interfaces (length N+1)

    // Constructor
    public Simulation(SimParams simParams)
    {
        Params = simParams;

        _h = new float[Params.NumCells];
        _q = new float[Params.NumCells];
        _hNew = new float[Params.NumCells];
        _qNew = new float[Params.NumCells];
        _fh = new float[Params.NumCells + 1];
        _fq = new float[Params.NumCells + 1];
    }

    // Simulation
    private float _lastInitialDepth;
    private float _lastInitialVelocity;
    public void InitialiseState(float initialDepth, float initialVelocity)
    {
        _lastInitialDepth = initialDepth;
        _lastInitialVelocity = initialVelocity;
        // if (DamBreakEnabled)
        // {
        //     int splitIndex = (int)(DamBreakSplit * NumCells);
        //     for (int i = 0; i < NumCells; i++)
        //     {
        //         if (i < splitIndex)
        //         {
        //             _h[i] = DamBreakLeftDepth;
        //         }
        //         else
        //         {
        //             _h[i] = DamBreakRightDepth;
        //         }
        //         _q[i] = 0.0f;
        //     }
        // }
        // else
        // {
        for (int i = 0; i < Params.NumCells; i++)
        {
            _h[i] = initialDepth;
            _q[i] = initialDepth * initialVelocity;
        }
        // }
    }

    // Simulation
    public void Step(float delta, World3D world, CrossSectionSampler crossSectionSampler, Vector3 originWorld, Vector3 channelAxis, float dx)
    {
        if (delta <= 0) return;

        float lambdaMax = ComputeMaxWaveSpeed();

        // Determine timestep and substeps
        float dtCfl = (lambdaMax > Params.MinDepth) ? (Params.Cfl * Params.Dx / lambdaMax) : delta;
        int substeps = (int)Math.Ceiling(delta / dtCfl);
        substeps = Math.Clamp(substeps, 1, Params.SubstepsMax);
        float dt = delta / substeps;

        LastSubsteps = substeps;

        // Run substeps
        for (int s = 0; s < substeps; s++)
        {
            ComputeFluxes();
            UpdateState(dt, world, crossSectionSampler, originWorld, channelAxis, dx);
        }
    }

    private float ComputeMaxWaveSpeed()
    {
        float maxSpeed = 0.0f;
        float maxDepth = 0.0f;

        for (int i = 0; i < Params.NumCells; i++)
        {
            float h = _h[i];
            if (h > Params.MinDepth)
            {
                float u = _q[i] / h;
                float c = MathF.Sqrt(Params.G * h);
                float speed = MathF.Abs(u) + c;
                maxSpeed = MathF.Max(maxSpeed, speed);
                maxDepth = MathF.Max(maxDepth, h);
            }
        }

        MaxWaveSpeed = maxSpeed;
        MaxDepth = maxDepth;

        return maxSpeed;
    }

    private void ComputeFluxes()
    {
        // Interior interfaces
        for (int i = 0; i <= Params.NumCells; i++)
        {
            float hL, qL, hR, qR;

            if (i == 0) // Left boundary (upstream)
            {
                GetLeftBoundaryState(out hL, out qL);
                hR = _h[0];
                qR = _q[0];
            }
            else if (i == Params.NumCells) // Right boundary (downstream)
            {
                hL = _h[Params.NumCells - 1];
                qL = _q[Params.NumCells - 1];
                GetRightBoundaryState(out hR, out qR);
            }
            else // Interior interface
            {
                hL = _h[i - 1];
                qL = _q[i - 1];
                hR = _h[i];
                qR = _q[i];
            }

            ComputeRusanovFlux(hL, qL, hR, qR, out _fh[i], out _fq[i]);
        }
    }

    private void UpdateState(float dt, World3D world,
        CrossSectionSampler crossSectionSampler, Vector3 originWorld,
        Vector3 channelAxis, float dx)
    {
        float dtDx = dt / Params.Dx;

        for (int i = 0; i < Params.NumCells; i++)
        {
            // Flux differences
            float dhFlux = dtDx * (_fh[i + 1] - _fh[i]);
            float dqFlux = dtDx * (_fq[i + 1] - _fq[i]);

            // Source terms
            float h = _h[i];
            float sourceQ = 0.0f;

            if (h > Params.MinDepth)
            {
                float u = _q[i] / h;
                float sf = ComputeFrictionSlope(i, h, u, world, crossSectionSampler, originWorld, channelAxis, dx);
                sourceQ = dt * Params.G * h * (Params.BedSlope - sf);
            }

            _hNew[i] = _h[i] - dhFlux;
            _qNew[i] = _q[i] - dqFlux + sourceQ;

            _hNew[i] = MathF.Max(_hNew[i], 0.0f);

            // Dry cell handling
            if (_hNew[i] < Params.MinDepth)
            {
                _hNew[i] = 0.0f;
                _qNew[i] = 0.0f;
            }

            // Velocity limiting for stability (optional safety)
            // if (_hNew[i] > Params.MinDepth)
            // {
            //     float uNew = _qNew[i] / _hNew[i];
            //     float maxU = 50.0f;
            //     if (MathF.Abs(uNew) > maxU)
            //     {
            //         _qNew[i] = MathF.Sign(uNew) * maxU * _hNew[i];
            //     }
            // }
        }

        (_h, _hNew) = (_hNew, _h);
        (_q, _qNew) = (_qNew, _q);
    }

    private void ComputeRusanovFlux(float hL, float qL, float hR, float qR, out float fh, out float fq)
    {
        // Compute velocities (with dry cell handling)
        float uL = (hL > Params.MinDepth) ? qL / hL : 0.0f;
        float uR = (hR > Params.MinDepth) ? qR / hR : 0.0f;

        // Wave speeds
        float cL = (hL > Params.MinDepth) ? MathF.Sqrt(Params.G * hL) : 0.0f;
        float cR = (hR > Params.MinDepth) ? MathF.Sqrt(Params.G * hR) : 0.0f;

        // Max wave speed for Rusanov dissipation
        float alpha = MathF.Max(MathF.Abs(uL) + cL, MathF.Abs(uR) + cR);

        // Flux function F(h, q) = [q, q²/h + 0.5*g*h²]
        float fhL = qL;
        float fqL = (hL > Params.MinDepth) ? (qL * qL / hL + 0.5f * Params.G * hL * hL) : 0.0f;

        float fhR = qR;
        float fqR = (hR > Params.MinDepth) ? (qR * qR / hR + 0.5f * Params.G * hR * hR) : 0.0f;

        // Rusanov flux: F = 0.5*(F_L + F_R) - 0.5*alpha*(U_R - U_L)
        fh = 0.5f * (fhL + fhR) - 0.5f * alpha * (hR - hL);
        fq = 0.5f * (fqL + fqR) - 0.5f * alpha * (qR - qL);
    }

    private void GetLeftBoundaryState(out float h, out float q)
    {
        if (InflowEnabled)
        {
            h = _h[0];
            q = InflowQPerWidth;
        }
        else
        {
            h = _h[0];
            q = _q[0];
        }
    }

    private void GetRightBoundaryState(out float h, out float q)
    {
        switch (OutflowModeValue)
        {
            case OutflowMode.FixedDepth:
                h = OutflowFixedDepth;
                q = _q[Params.NumCells - 1];
                break;
            case OutflowMode.OpenCopy:
            default:
                h = _h[Params.NumCells - 1];
                q = _q[Params.NumCells - 1];
                break;
        }
    }

    private float ComputeFrictionSlope(int cellIndex, float h, float u,
        World3D world, CrossSectionSampler crossSectionSampler,
        Vector3 originWorld, Vector3 channelAxis, float dx)
    {
        if (h < Params.MinDepth || MathF.Abs(Params.ManningN) < 1e-8f)
        {
            return 0.0f;
        }

        float hydraulicRadius;
        if (world != null && crossSectionSampler != null)
        {
            float cellCenterX = (cellIndex + 0.5f) * dx;
            Vector3 cellCenterPos = originWorld + channelAxis * cellCenterX;

            var props = crossSectionSampler.SampleCrossSection(cellCenterPos, h, 1.0f);
            hydraulicRadius = props.HydraulicRadius;

            // Fallback for invalid hydraulic radius
            if (hydraulicRadius <= 0.0f)
            {
                hydraulicRadius = h / 3.0f;
            }
        }
        else
        {
            // Fallback: assume rectangular channel
            float B = 1.0f;
            float area = B * h;
            float wettedPerimeter = B + 2.0f * h;
            hydraulicRadius = area / wettedPerimeter;
        }

        // Manning friction slope: Sf = n² * u² / R^(4/3)
        // With sign to oppose flow
        float r43 = MathF.Pow(hydraulicRadius, 4.0f / 3.0f);
        if (r43 < 1e-8f) r43 = 1e-8f;
        float sf = Params.ManningN * Params.ManningN * u * MathF.Abs(u) / r43;
        return sf;
    }

    // Lifecycle
    public void Reset()
    {
        InitialiseState(_lastInitialDepth, _lastInitialVelocity);
    }

    public void SetDepth(int cellIndex, float value)
    {
        if (cellIndex < 0 || cellIndex >= Params.NumCells) return;
        _h[cellIndex] = value;
    }

    public void SetDischarge(int cellIndex, float value)
    {
        if (cellIndex < 0 || cellIndex >= Params.NumCells) return;
        _q[cellIndex] = value;
    }

    public void SetState(int cellIndex, float depth, float discharge)
    {
        if (cellIndex < 0 || cellIndex >= Params.NumCells) return;
        _h[cellIndex] = depth;
        _q[cellIndex] = discharge;
    }

    public float GetVelocity(int cellIndex)
    {
        if (cellIndex < 0 || cellIndex >= Params.NumCells) return 0.0f;
        if (_h[cellIndex] < Params.MinDepth) return 0.0f;
        return _q[cellIndex] / _h[cellIndex];
    }
}