/*
 * Copyright (c) 2018 Demerzel Solutions Limited
 * This file is part of the Nethermind library.
 *
 * The Nethermind library is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * The Nethermind library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.
 */

using System.Threading.Tasks;
using Nethermind.Core;
using Nethermind.DataMarketplace.Consumers.Deposits.Domain;
using Nethermind.DataMarketplace.Consumers.Deposits.Repositories;
using Nethermind.DataMarketplace.Consumers.Shared.Services.Models;
using Nethermind.DataMarketplace.Core.Domain;
using Nethermind.DataMarketplace.Core.Services;
using Nethermind.Logging;

namespace Nethermind.DataMarketplace.Consumers.Refunds.Services
{
    public class RefundClaimant : IRefundClaimant
    {
        private readonly IRefundService _refundService;
        private readonly INdmBlockchainBridge _blockchainBridge;
        private readonly IDepositDetailsRepository _depositRepository;
        private readonly ITransactionVerifier _transactionVerifier;
        private readonly IGasPriceService _gasPriceService;
        private readonly ITimestamper _timestamper;
        private readonly ILogger _logger;

        public RefundClaimant(IRefundService refundService, INdmBlockchainBridge blockchainBridge,
            IDepositDetailsRepository depositRepository, ITransactionVerifier transactionVerifier,
            IGasPriceService gasPriceService, ITimestamper timestamper, ILogManager logManager)
        {
            _refundService = refundService;
            _blockchainBridge = blockchainBridge;
            _depositRepository = depositRepository;
            _transactionVerifier = transactionVerifier;
            _gasPriceService = gasPriceService;
            _timestamper = timestamper;
            _logger = logManager.GetClassLogger();
        }

        public async Task<RefundClaimStatus> TryClaimRefundAsync(DepositDetails deposit, Address refundTo)
        {
            var now = _timestamper.EpochSeconds;
            if (!deposit.CanClaimRefund(now))
            {
                return RefundClaimStatus.Empty;
            }
            
            var latestBlock = await _blockchainBridge.GetLatestBlockAsync();
            now = (ulong) latestBlock.Timestamp;
            if (!deposit.CanClaimRefund(now))
            {
                return RefundClaimStatus.Empty;
            }
            
            var depositId = deposit.Deposit.Id;
            var transactionHash = deposit.ClaimedRefundTransaction?.Hash;
            if (transactionHash is null)
            {
                var provider = deposit.DataAsset.Provider.Address;
                var refundClaim = new RefundClaim(depositId, deposit.DataAsset.Id, deposit.Deposit.Units,
                    deposit.Deposit.Value, deposit.Deposit.ExpiryTime, deposit.Pepper, provider, refundTo);
                var gasPrice = await _gasPriceService.GetCurrentAsync();
                transactionHash = await _refundService.ClaimRefundAsync(refundTo, refundClaim, gasPrice);
                if (transactionHash is null)
                {
                    if (_logger.IsError) _logger.Error("There was an error when trying to claim refund (no transaction hash returned).");
                    return RefundClaimStatus.Empty;
                }

                deposit.SetClaimedRefundTransaction(new TransactionInfo(transactionHash, 0, gasPrice,
                    _refundService.GasLimit, _timestamper.EpochSeconds));
                await _depositRepository.UpdateAsync(deposit);
                if (_logger.IsInfo) _logger.Info($"Claimed a refund for deposit: '{depositId}', gas price: {gasPrice} wei, transaction hash: '{transactionHash}' (awaits a confirmation).");
            }

            var confirmed = await TryConfirmClaimAsync(deposit, string.Empty);

            return confirmed
                ? RefundClaimStatus.Confirmed(transactionHash)
                : RefundClaimStatus.Unconfirmed(transactionHash);
        }

        public async Task<RefundClaimStatus> TryClaimEarlyRefundAsync(DepositDetails deposit, Address refundTo)
        {
            var now = _timestamper.EpochSeconds;
            if (!deposit.CanClaimEarlyRefund(now))
            {
                return RefundClaimStatus.Empty;
            }
            
            var latestBlock = await _blockchainBridge.GetLatestBlockAsync();
            now = (ulong) latestBlock.Timestamp;
            if (!deposit.CanClaimEarlyRefund(now))
            {
                return RefundClaimStatus.Empty;
            }
            
            var depositId = deposit.Deposit.Id;
            var transactionHash = deposit.ClaimedRefundTransaction?.Hash;
            if (transactionHash is null)
            {
                var provider = deposit.DataAsset.Provider.Address;
                var ticket = deposit.EarlyRefundTicket;
                var earlyRefundClaim = new EarlyRefundClaim(ticket.DepositId, deposit.DataAsset.Id,
                    deposit.Deposit.Units, deposit.Deposit.Value, deposit.Deposit.ExpiryTime, deposit.Pepper, provider,
                    ticket.ClaimableAfter, ticket.Signature, refundTo);
                var gasPrice = await _gasPriceService.GetCurrentAsync();
                transactionHash = await _refundService.ClaimEarlyRefundAsync(refundTo, earlyRefundClaim, gasPrice);
                if (transactionHash is null)
                {
                    if (_logger.IsError) _logger.Error("There was an error when trying to claim early refund (no transaction hash returned).");
                    return RefundClaimStatus.Empty;
                }

                deposit.SetClaimedRefundTransaction(new TransactionInfo(transactionHash, 0, gasPrice,
                    _refundService.GasLimit, _timestamper.EpochSeconds));
                await _depositRepository.UpdateAsync(deposit);
                if (_logger.IsInfo) _logger.Info($"Claimed an early refund for deposit: '{depositId}', gas price: {gasPrice} wei, transaction hash: '{transactionHash}' (awaits a confirmation).");
            }

            var confirmed = await TryConfirmClaimAsync(deposit, "early ");
            
            return confirmed
                ? RefundClaimStatus.Confirmed(transactionHash)
                : RefundClaimStatus.Unconfirmed(transactionHash);
        }

        private async Task<bool> TryConfirmClaimAsync(DepositDetails deposit, string type)
        {
            var claimType = $"{type}refund";
            var depositId = deposit.Id;
            var transactionHash = deposit.ClaimedRefundTransaction.Hash;
            var transaction  = await _blockchainBridge.GetTransactionAsync(transactionHash);
            if (transaction is null)
            {
                if (_logger.IsInfo) _logger.Info($"Transaction was not found for hash: '{transactionHash}' for deposit: '{depositId}' to claim the {claimType}.");
                return false;
            }
            
            if (transaction.IsPending)
            {
                if (_logger.IsInfo) _logger.Info($"Transaction with hash: '{transactionHash}' for deposit: '{deposit.Id}' ({claimType}) is still pending.");
                return false;
            }

            if (deposit.ClaimedRefundTransaction.State == TransactionState.Pending)
            {
                deposit.ClaimedRefundTransaction.SetIncluded();
                if (_logger.IsInfo) _logger.Info($"Transaction with hash: '{transactionHash}' for deposit: '{deposit.Id}' ({claimType}) was included into block: {transaction.BlockNumber}.");
                await _depositRepository.UpdateAsync(deposit);
            }
            
            if (_logger.IsInfo) _logger.Info($"Trying to claim the {claimType} (transaction hash: '{transactionHash}') for deposit: '{depositId}'.");
            var verifierResult = await _transactionVerifier.VerifyAsync(transaction);
            if (!verifierResult.BlockFound)
            {
                if (_logger.IsWarn) _logger.Warn($"Block number: {transaction.BlockNumber}, hash: '{transaction.BlockHash}' was not found for transaction hash: '{transactionHash}' - {claimType} claim for deposit: '{depositId}' will not confirmed.");
                return false;
            }
            
            if (_logger.IsInfo) _logger.Info($"The {claimType} claim (transaction hash: '{transactionHash}') for deposit: '{depositId}' has {verifierResult.Confirmations} confirmations (required at least {verifierResult.RequiredConfirmations}).");
            if (!verifierResult.Confirmed)
            {
                return false;
            }
            
            deposit.SetRefundClaimed();
            await _depositRepository.UpdateAsync(deposit);
            if (_logger.IsInfo) _logger.Info($"The {claimType} claim (transaction hash: '{transactionHash}') for deposit: '{depositId}' has been confirmed.");

            return true;
        }
    }
}