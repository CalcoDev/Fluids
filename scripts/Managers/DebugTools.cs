using Godot;
using System;

namespace Fluids.scripts.Managers;

public partial class DebugTools : Node
{
    public override void _Process(double delta) {
        base._Process(delta);

        if (Input.IsActionJustPressed("screenshot"))
        {
            // make the game take a screenshot and save it in the "res://screenshots" folder
            string screenshotsDir = "res://_screenshots";
            if (!DirAccess.DirExistsAbsolute(screenshotsDir))
            {
                DirAccess.MakeDirAbsolute(screenshotsDir);
            }

            string fileName = $"{screenshotsDir}/screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var image = GetViewport().GetTexture().GetImage();
            image.SavePng(fileName);
            GD.Print($"Screenshot saved to: {fileName}");
        }
    }
}
