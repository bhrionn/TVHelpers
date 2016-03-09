﻿//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

namespace MediaAppSample.Core.Models
{
    public class TvSeriesModel : ContentItemBase
    {
        #region Constructor

        public TvSeriesModel()
        {
            this.Seasons = new ModelList<SeasonModel>();
        }

        #endregion

        #region Properties

        public string ReleaseDate { get; set; }

        public override int Year => 2015;

        public ModelList<SeasonModel> Seasons { get; set; }

        #endregion
    }
}