﻿using System.Numerics;

namespace Nevermind.Core.Difficulty
{
    public class MainNetworkDifficultyCalculator : FrontierDifficultyCalculator
    {
        public const long HomesteadBlockNumber = 1150000;

        private readonly HomesteadDifficultyCalculator _homestead = new HomesteadDifficultyCalculator();

        protected internal override BigInteger TimeAdjustment(BigInteger parentTimestamp, BigInteger currentTimestamp, BigInteger blockNumber)
        {
            if (blockNumber < HomesteadBlockNumber)
            {
                return base.TimeAdjustment(parentTimestamp, currentTimestamp, blockNumber);
            }

            return _homestead.TimeAdjustment(parentTimestamp, currentTimestamp, blockNumber);
        }
    }
}