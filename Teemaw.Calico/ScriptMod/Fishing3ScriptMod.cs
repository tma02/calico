﻿using GDWeave;
using GDWeave.Godot;
using GDWeave.Godot.Variants;
using GDWeave.Modding;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico.ScriptMod;

public class Fishing3ScriptMod(IModInterface mod): IScriptMod
{
    public bool ShouldRun(string path) => path == "res://Scenes/Minigames/Fishing3/fishing3.gdc";

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        MultiTokenWaiter mainProgressWaiter = new([
            t => t is IdentifierToken { Name: "main_progress" },
            t => t.Type is OpAssignAdd,
            t => t is IdentifierToken { Name: "reel_speed" },
        ]);
        
        MultiTokenWaiter badProgressWaiter = new([
            t => t is IdentifierToken { Name: "bad_progress" },
            t => t.Type is OpAssignAdd,
            t => t is IdentifierToken { Name: "bad_speed" },
        ]);
        
        mod.Logger.Information($"[calico.Fishing3ScriptMod] Patching {path}");
        
        var patchFlags = new Dictionary<string, bool>
        {
            ["main_progress"] = false,
            ["bad_progress"] = false
        };
        
        foreach (var t in tokens)
        {
            yield return t;
            // We already yielded the identifier, now multiply by 2
            if (mainProgressWaiter.Check(t))
            {
                yield return new Token(OpMul);
                yield return new ConstantToken(new IntVariant(2));
                patchFlags["main_progress"] = true;
                mod.Logger.Information("[calico.HeldItemScriptMod] main_progress patch OK");
            }
            else if (badProgressWaiter.Check(t))
            {
                yield return new Token(OpMul);
                yield return new ConstantToken(new IntVariant(2));
                patchFlags["bad_progress"] = true;
                mod.Logger.Information("[calico.HeldItemScriptMod] bad_progress patch OK");
            }
        }
        
        foreach (var patch in patchFlags)
        {
            if (!patch.Value)
            {
                mod.Logger.Error($"[calico.Fishing3ScriptMod] FAIL: {patch.Key} patch not applied");
            }
        }
    }
}