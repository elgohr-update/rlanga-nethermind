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

namespace Nethermind.JsonRpc.Module
{
    using System.Collections.Generic;
    using Nethermind.JsonRpc.DataModel;

    public interface INethmModule : IModule
    {
        ResultWrapper<IEnumerable<string>> nethm_getCompilers();
        ResultWrapper<Data> nethm_compileLLL(string code);
        ResultWrapper<string> nethm_compileSolidity(string parameters);
        ResultWrapper<Data> nethm_compileSerpent(string code);
    }
}