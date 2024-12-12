using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod.LobbyQol;

public static class LobbyQolEscMenuScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("LobbyQolEscMenuScriptMod")
            .Patching("res://Scenes/HUD/Esc Menu/esc_menu.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("globals")
                .Matching(CreateGlobalsPattern())
                .Do(Append)
                .With(
                    """
                    
                    var calico_code_show
                    var calico_code_display
                    var calico_code_label
                    var calico_code_shown = false
                    
                    func calico_on_show_code_pressed():
                    	calico_code_shown = true
                    
                    func calico_on_copy_paste_pressed():
                    	OS.set_clipboard(str(Network.CALICO_LOBBY_ID.to_upper()))

                    """
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("ready")
                .Matching(CreateFunctionDefinitionPattern("_ready"))
                .Do(Append)
                .With(
                    """

                    calico_code_show = $VBoxContainer/code_show.duplicate()
                    calico_code_show.text = "Calico: Show Lobby ID"
                    calico_code_show.get_node("TooltipNode").header = "Show Lobby ID"
                    calico_code_show.get_node("TooltipNode").body = "Shows the current game's lobby ID. Other lobbies can never have the same ID. Players with Calico can join with this ID!"
                    calico_code_show.disconnect("pressed", self, "_on_code_pressed")
                    calico_code_show.connect("pressed", self, "calico_on_show_code_pressed")
                    calico_code_display = $VBoxContainer/code_display.duplicate()
                    calico_code_display.get_node("copy_paste").disconnect("pressed", self, "_on_copy_paste_pressed")
                    calico_code_display.get_node("copy_paste").get_node("TooltipNode").header = "Copy Lobby ID"
                    calico_code_display.get_node("copy_paste").get_node("TooltipNode").body = "Copies the lobby ID to your clipboard."
                    calico_code_display.get_node("copy_paste").connect("pressed", self, "calico_on_copy_paste_pressed")
                    calico_code_label = calico_code_display.get_node("Panel").get_node("code")
                    $VBoxContainer.add_child(calico_code_show)
                    $VBoxContainer.move_child(calico_code_show, 3)
                    $VBoxContainer.add_child(calico_code_display)
                    $VBoxContainer.move_child(calico_code_display, 4)

                    """, 1
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("process")
                .Matching(CreateFunctionDefinitionPattern("_process", ["delta"]))
                .Do(Append)
                .With(
                    """
                    
                    calico_code_show.visible = !calico_code_shown && !Network.PLAYING_OFFLINE && Network.CODE_ENABLED
                    calico_code_display.visible = calico_code_shown
                    calico_code_label.text = str(Network.CALICO_LOBBY_ID)

                    """, 1
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("open")
                .Matching(CreateFunctionDefinitionPattern("_open"))
                .Do(Append)
                .With(
                    """

                    calico_code_shown = false

                    """, 1
                )
            )
            .Build();
    }
}