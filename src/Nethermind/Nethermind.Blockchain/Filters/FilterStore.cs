﻿/*
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nethermind.Blockchain.Filters.Topics;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Dirichlet.Numerics;
using NLog.Filters;

namespace Nethermind.Blockchain.Filters
{
    public class FilterStore : IFilterStore
    {
        private int _nextFilterId;

        private readonly ConcurrentDictionary<int, FilterBase> _filters = new ConcurrentDictionary<int, FilterBase>();

        public bool FilterExists(int filterId) => _filters.ContainsKey(filterId);

        public FilterType GetFilterType(int filterId)
        {
            /* so far ok to use block filter if none */
            _filters.TryGetValue(filterId, out FilterBase filter);
            return filter?.GetType() == typeof(LogFilter) ? FilterType.LogFilter : FilterType.BlockFilter;
        }

        public T[] GetFilters<T>() where T : FilterBase
        {
            return _filters.Select(f => f.Value).OfType<T>().ToArray();
        }

        public BlockFilter CreateBlockFilter(UInt256 startBlockNumber, bool setId = true)
        {
            var filterId = setId ? GetFilterId() : 0;
            var blockFilter = new BlockFilter(filterId, startBlockNumber);
            return blockFilter;
        }

        public LogFilter CreateLogFilter(FilterBlock fromBlock, FilterBlock toBlock,
            object address = null, IEnumerable<object> topics = null, bool setId = true)
        {
            var filterId = setId ? GetFilterId() : 0;
            var filter = new LogFilter(filterId, fromBlock, toBlock,
                GetAddress(address), GetTopicsFilter(topics));

            return filter;
        }

        public void RemoveFilter(int filterId)
        {
            _filters.TryRemove(filterId, out _);
            FilterRemoved?.Invoke(this, new FilterEventArgs(filterId));
        }

        public event EventHandler<FilterEventArgs> FilterRemoved;

        public void SaveFilter(FilterBase filter)
        {
            if (_filters.ContainsKey(filter.Id))
            {
                throw new InvalidOperationException($"Filter with ID {filter.Id} already exists");
            }

            _nextFilterId = Math.Max(filter.Id + 1, _nextFilterId);
            _filters[filter.Id] = filter;
        }

        private int GetFilterId() => _nextFilterId++;

        private TopicsFilter GetTopicsFilter(IEnumerable<object> topics = null)
        {
            if (topics == null)
            {
                return TopicsFilter.AnyTopic;
            }

            var filterTopics = GetFilterTopics(topics);
            var expressions = new List<TopicExpression>();

            for (int i = 0; i < filterTopics.Length; i++)
            {
                expressions.Add(GetTopicExpression(filterTopics[i]));
            }

            return new TopicsFilter(expressions.ToArray());
        }

        private TopicExpression GetTopicExpression(FilterTopic filterTopic)
        {
            if (filterTopic == null)
            {
                return new AnyTopic();
            }

            return new OrExpression(new[]
            {
                GetTopicExpression(filterTopic.First),
                GetTopicExpression(filterTopic.Second)
            });
        }

        private TopicExpression GetTopicExpression(Keccak topic)
        {
            if (topic == null)
            {
                return new AnyTopic();
            }

            return new SpecificTopic(topic);
        }

        private static AddressFilter GetAddress(object address)
        {
            if (address is null)
            {
                return AddressFilter.AnyAddress; 
            }

            if (address is string s)
            {
                return new AddressFilter(new Address(s));
            }
            
            if (address is IEnumerable<string> e)
            {
                return new AddressFilter(e.Select(a => new Address(a)).ToHashSet());
            }
            
            throw new InvalidDataException("Invalid address filter format");
        }

        private static FilterTopic[] GetFilterTopics(IEnumerable<object> topics)
        {
            return topics?.Select(GetTopic).ToArray();
        }

        private static FilterTopic GetTopic(object obj)
        {
            switch (obj)
            {
                case null:
                    return null;
                case string topic:
                    return new FilterTopic
                    {
                        First = new Keccak(topic)
                    };
            }

            var topics = (obj as IEnumerable<string>)?.ToList();
            var first = topics?.FirstOrDefault();
            var second = topics?.Skip(1).FirstOrDefault();

            return new FilterTopic
            {
                First = first is null ? null : new Keccak(first),
                Second = second is null ? null : new Keccak(second)
            };
        }
    }
}