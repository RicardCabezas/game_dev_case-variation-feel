namespace Game.GamePlay.Entities
{
    /// <summary>Shared animator parameter names and gameplay world limits.</summary>
    public static class Constants
    {
        /// <summary>Animator parameter groups.</summary>
        public static class Animator
        {
            /// <summary>Animator parameters expected by hero prefabs.</summary>
            public static class Hero
            {
                /// <summary>Float parameter indicating movement speed.</summary>
                public const string Speed = "Speed";

                /// <summary>Trigger parameter for hero attack presentation.</summary>
                public const string Attack = "Attack";

                /// <summary>Boolean parameter that keeps hero death presentation active until restart.</summary>
                public const string Death = "Death";
            }

            /// <summary>Animator parameters expected by bee enemy prefabs.</summary>
            public static class Bee
            {
                /// <summary>Boolean parameter indicating chase movement.</summary>
                public const string IsMoving = "IsMoving";

                /// <summary>Trigger parameter for nonlethal hit presentation.</summary>
                public const string Damage = "Damage";

                /// <summary>Trigger parameter for enemy attack presentation.</summary>
                public const string Attack = "Attack";

                /// <summary>Trigger parameter for enemy death presentation.</summary>
                public const string Death = "Death";
            }
        }

        public static class World
        {
            /// <summary>Inclusive X/Z world-unit limit for arena boundaries and spawned gameplay objects.</summary>
            public const float ArenaLimit = 14.3f;
        }
    }
}
