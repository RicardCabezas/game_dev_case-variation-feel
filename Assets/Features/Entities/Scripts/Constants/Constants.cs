namespace Game.GamePlay.Entities
{
    /// <summary>Centralized animator parameter names used by entity presentation.</summary>
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

                /// <summary>Trigger parameter for hero death presentation.</summary>
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
            }
        }
    }
}
