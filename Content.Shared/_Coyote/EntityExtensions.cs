using System.Runtime.CompilerServices;

namespace Content.Shared._COYOTE;

public static class EntityExtensions
{
    // KodiCraft:
    // For some reason this is not a part of the Entity type in Robust as of writing (02-02-2026)
    // It's not strictly necessary and probably not great, but I do really like having the type safety of being able to
    // guarantee that an entity has some given components.
    // Definitely worth contributing this upstream and removing this at some point, in my opinion.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity<T2, T1> Swap<T1, T2>(this Entity<T1, T2> ent)
        where T1 : IComponent?
        where T2 : IComponent?
    {
        return new Entity<T2, T1>(ent.Owner, ent.Comp2, ent.Comp1);
    }
}
