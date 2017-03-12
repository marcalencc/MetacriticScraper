﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetacriticScraper.MediaData
{
    public class Rating
    {
        public Rating(short criticRating, short userRating,
            short criticReviewCount, short userReviewCount)
        {
            CriticRating = criticRating;
            UserRating = userRating;
            CriticReviewCount = criticReviewCount;
            UserReviewCount = userReviewCount;
        }

        public Rating (short criticRating, short criticReviewCount): this (criticRating, -1, criticReviewCount, -1)
        {
        }

        public short CriticRating { get; set; }
        public short CriticReviewCount { get; set; }
        public short UserRating { get; set; }
        public short UserReviewCount { get; set; }
    }
}
