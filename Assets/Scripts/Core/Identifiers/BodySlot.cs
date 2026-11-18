namespace CollisionJam.Core
{
    /// <summary>
    /// Stable program slots, with left and right referring to the character's own sides.
    /// Slot names do not define item compatibility, equipment dependencies, or a complete human body.
    /// Those conditions belong to equipment rules. None is invalid for equipment commands.
    /// </summary>
    public enum BodySlot
    {
        None = 0,
        Head = 1,
        HeadCover = 2,
        LeftHand = 3,
        RightHand = 4,
        // Values 5/6 belonged to retired left/right leg slots and must stay invalid.
        Legs = 7
    }
}
