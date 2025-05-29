using Sandbox.Game.Entities;

namespace WarpDriveClient

{
    public static class WarpSounds
    {
        // Sound played when warp starts charging
        public static readonly MySoundPair WarpCharge = new MySoundPair("quantum_charging");

        // Looping sound while in warp
        public static readonly MySoundPair WarpTravel = new MySoundPair("WarpLoopTrack");

        // Sound played when warp stops (either complete or interrupted)
        public static readonly MySoundPair WarpExit = new MySoundPair("quantum_jumpout");

        // Alert sound when collision is detected
        public static readonly MySoundPair CollisionWarning = new MySoundPair("WarpDrive_Collision");

        // Optional UI or notification sound when warp fails to start
        public static readonly MySoundPair WarpError = new MySoundPair("WarpDrive_Error");
    }
}
