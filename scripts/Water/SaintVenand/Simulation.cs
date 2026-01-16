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

    // Configuration
    public SimParams Params;

    public ReadOnlySpan<float> H => _h;
    public ReadOnlySpan<float> Q => _q;

    // Stats
    public float MaxWaveSpeed { get; private set; }
    public float MaxDepth { get; private set; }
    public int LastSubsteps { get; private set; }

    // Internals
    private readonly float[] _h;      // depth
    private readonly float[] _q;      // discharge per unit width (q = h * u)
    private readonly float[] _hNew;
    private readonly float[] _qNew;
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
        _fh = new float[Params.NumCells];
        _fq = new float[Params.NumCells];
    }

    // Simulation
    public void Step()
    {

    }
}