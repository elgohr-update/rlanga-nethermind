﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethermind.BeaconNode.Configuration;
using Nethermind.BeaconNode.Containers;
using Nethermind.BeaconNode.Ssz;
using Nethermind.Core2.Types;

namespace Nethermind.BeaconNode
{
    public class Genesis
    {
        private readonly BeaconStateAccessor _beaconStateAccessor;
        private readonly BeaconStateTransition _beaconStateTransition;
        private readonly ChainConstants _chainConstants;
        private readonly IOptionsMonitor<GweiValues> _gweiValueOptions;
        private readonly IOptionsMonitor<InitialValues> _initialValueOptions;
        private readonly ILogger _logger;
        private readonly IOptionsMonitor<MaxOperationsPerBlock> _maxOperationsPerBlockOptions;
        private readonly IOptionsMonitor<MiscellaneousParameters> _miscellaneousParameterOptions;
        private readonly IOptionsMonitor<StateListLengths> _stateListLengthOptions;
        private readonly IOptionsMonitor<TimeParameters> _timeParameterOptions;

        public Genesis(ILogger<Genesis> logger,
            ChainConstants chainConstants,
            IOptionsMonitor<MiscellaneousParameters> miscellaneousParameterOptions,
            IOptionsMonitor<GweiValues> gweiValueOptions,
            IOptionsMonitor<InitialValues> initialValueOptions,
            IOptionsMonitor<TimeParameters> timeParameterOptions,
            IOptionsMonitor<StateListLengths> stateListLengthOptions,
            IOptionsMonitor<MaxOperationsPerBlock> maxOperationsPerBlockOptions,
            BeaconStateAccessor beaconStateAccessor,
            BeaconStateTransition beaconStateTransition)
        {
            _logger = logger;
            _beaconStateAccessor = beaconStateAccessor;
            _beaconStateTransition = beaconStateTransition;
            _chainConstants = chainConstants;
            _miscellaneousParameterOptions = miscellaneousParameterOptions;
            _gweiValueOptions = gweiValueOptions;
            _initialValueOptions = initialValueOptions;
            _timeParameterOptions = timeParameterOptions;
            _stateListLengthOptions = stateListLengthOptions;
            _maxOperationsPerBlockOptions = maxOperationsPerBlockOptions;
        }

        public BeaconState InitializeBeaconStateFromEth1(Hash32 eth1BlockHash, ulong eth1Timestamp, IEnumerable<Deposit> deposits)
        {
            _logger.LogDebug(Event.InitializeBeaconState, "Initialise beacon state from ETH1 block {Eth1BlockHash}, time {Eth1Timestamp}, with {DepositCount} deposits.", eth1BlockHash, eth1Timestamp, deposits.Count());

            GweiValues gweiValues = _gweiValueOptions.CurrentValue;
            InitialValues initialValues = _initialValueOptions.CurrentValue;
            TimeParameters timeParameters = _timeParameterOptions.CurrentValue;
            StateListLengths stateListLengths = _stateListLengthOptions.CurrentValue;

            ulong genesisTime = eth1Timestamp - (eth1Timestamp % _chainConstants.SecondsPerDay)
                + (2 * _chainConstants.SecondsPerDay);
            Eth1Data eth1Data = new Eth1Data((ulong)deposits.Count(), eth1BlockHash);
            BeaconBlockBody emptyBlockBody = new BeaconBlockBody();
            BeaconBlockHeader latestBlockHeader = new BeaconBlockHeader(emptyBlockBody.HashTreeRoot(_miscellaneousParameterOptions.CurrentValue, _maxOperationsPerBlockOptions.CurrentValue));
            BeaconState state = new BeaconState(genesisTime, 0, eth1Data, latestBlockHeader, timeParameters.SlotsPerHistoricalRoot, stateListLengths.EpochsPerHistoricalVector, stateListLengths.EpochsPerSlashingsVector, _chainConstants.JustificationBitsLength);

            // Process deposits
            var depositDataList = new List<DepositData>();
            foreach (Deposit deposit in deposits)
            {
                depositDataList.Add(deposit.Data);
                Hash32 depositRoot = depositDataList.HashTreeRoot(_chainConstants.MaximumDepositContracts);
                state.Eth1Data.SetDepositRoot(depositRoot);
                _beaconStateTransition.ProcessDeposit(state, deposit);
            }

            // Process activations
            for (int validatorIndex = 0; validatorIndex < state.Validators.Count; validatorIndex++)
            {
                Validator validator = state.Validators[validatorIndex];
                Gwei balance = state.Balances[validatorIndex];
                Gwei effectiveBalance = Gwei.Min(balance - (balance % gweiValues.EffectiveBalanceIncrement), gweiValues.MaximumEffectiveBalance);
                validator.SetEffectiveBalance(effectiveBalance);
                if (validator.EffectiveBalance == gweiValues.MaximumEffectiveBalance)
                {
                    validator.SetEligible(initialValues.GenesisEpoch);
                    validator.SetActive(initialValues.GenesisEpoch);
                }
            }

            return state;
        }

        public bool IsValidGenesisState(BeaconState state)
        {
            MiscellaneousParameters miscellaneousParameters = _miscellaneousParameterOptions.CurrentValue;
            InitialValues initialValues = _initialValueOptions.CurrentValue;

            if (state.GenesisTime < miscellaneousParameters.MinimumGenesisTime)
            {
                return false;
            }
            var activeValidatorIndices = _beaconStateAccessor.GetActiveValidatorIndices(state, initialValues.GenesisEpoch);
            if (activeValidatorIndices.Count < miscellaneousParameters.MinimumGenesisActiveValidatorCount)
            {
                return false;
            }
            return true;
        }
    }
}
