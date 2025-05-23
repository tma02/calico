﻿using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.GracefulDegradation.ModConflictCatalog;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod.GracefulDegradation;

public static class GracefulDegradationSplashScriptModFactory
{
    public static IScriptMod Create(IModInterface mi, Config config, ConfigFileSchema configFile)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mi)
            .Named("GracefulDegradationSplashScriptMod")
            .Patching("res://Scenes/Menus/Splash/splash.gdc")
            // This override should disable instead of enable
            .When(() => !config.ZzCompatOverrideMayCauseCrash)
            .AddRule(new TransformationRuleBuilder()
                .Named("compat_message")
                .Matching(
                    CreateFunctionDefinitionPattern("_on_AnimationPlayer_animation_finished",
                        ["anim_name"])
                )
                .When(() => AnyConflicts(mi, configFile))
                .Do(Append)
                .With(
                    $"""

                     PopupMessage._show_popup("{GetConflictMessage(mi, configFile)}", 0.1)

                     """, 1
                )
            )
            .Build();
    }
}