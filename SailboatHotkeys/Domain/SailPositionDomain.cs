using System;

namespace SailboatHotkeys.Domain
{
    public static class SailPositionDomain
    {
        public static readonly int[] ValidPositions = [0, 1, 2];

        public static int MaxPosition => ValidPositions[^1];

        public static int MinPosition => ValidPositions[0];

        public static int GetNextPosition(int currentPosition)
        {
            int currentIndex = Array.IndexOf(ValidPositions, currentPosition);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int nextIndex = (currentIndex + 1) % ValidPositions.Length;
            return ValidPositions[nextIndex];
        }
    }
}
