using GDWeave.Godot;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico.Util;

using MultiTokenChecks = Func<Token, bool>[];

public static class WaiterDefinitions
{
    /**
     * Creates a new array of checks which matches the GDScript `extends [Identifier]`
     */
    public static MultiTokenChecks CreateGlobalsChecks()
    {
        return [
            t => t.Type is PrExtends,
            t => t.Type is Identifier,
            t => t.Type is Newline
        ];
    }
}