﻿using System;
using System.Collections.Generic;
using System.Linq;
using MetacriticScraper.Scraper;
using MetacriticScraper.Interfaces;
using MetacriticScraper.MediaData;

namespace MetacriticScraper.RequestData
{
    public class TVShowRequestItem : RequestItem
    {
        private string m_season;

        public TVShowRequestItem(string id, string title, string thirdLevelRequest) :
            base(id, title, thirdLevelRequest)
        {
            MediaType = Constants.TvShowTypeId;
        }

        public TVShowRequestItem(string id, string title, string season, string thirdLevelRequest) :
            this(id, title, thirdLevelRequest)
        {
            m_season = season;
        }

        public override List<UrlResponsePair> Scrape()
        {
            Logger.Info("Scraping {0} urls for {1}", Urls.Count, SearchString);
            List<UrlResponsePair> responses = new List<UrlResponsePair>();
            foreach (string url in Urls)
            {
                var task = m_webUtils.HttpGet(Constants.MetacriticURL + "/" + url, Constants.MetacriticURL, 30000);
                responses.Add(new UrlResponsePair(url, task.Result));
            }

            return responses;
        }

        public override bool FilterValidUrls()
        {
            if (!String.IsNullOrEmpty(m_season))
            {
                Urls = m_autoResult.Where(r => this.Equals(r)).Select(r => r.Url + "/season-" + m_season).ToList();
            }
            else
            {
                Urls = m_autoResult.Where(r => this.Equals(r)).Select(r => r.Url).ToList();
            }

            SetThirdLevelRequest();
            Logger.Info("{0} urls filtered for {1}", Urls.Count, SearchString);
            return Urls.Count > 0;
        }

        public override IMetacriticData Parse(UrlResponsePair urlResponsePair)
        {
            string html = urlResponsePair.Response;
            if (String.IsNullOrEmpty(m_thirdLevelRequest))
            {
                TVShow tvShow = new TVShow();
                tvShow.Title = ParseItem(ref html, @"<span itemprop=""name"">", @"</span>");
                tvShow.Season = Int32.Parse(ParseItem(ref html, @"Season ", @"</span>"));

                tvShow.Studio = ParseItem(ref html, @"<span itemprop=""name"">", @"</span>");

                short criticRating = 0;
                short criticRatingCount = 0;
                if (short.TryParse(ParseItem(ref html, @"<span itemprop=""ratingValue"">", @"</span>"), out criticRating))
                {
                    criticRatingCount = Int16.Parse(ParseItem(ref html, @"<span itemprop=""reviewCount"">", @"</span>"));
                }

                float userRating = 0;
                short userRatingCount = 0;
                html = html.Substring(html.IndexOf("metascore_w user large"));
                if (float.TryParse(ParseItem(ref html, @""">", @"</div>"), out userRating))
                {
                    userRatingCount = Int16.Parse(ParseItem(ref html, @"user-reviews"">", @" Ratings"));
                }

                tvShow.Rating = new Rating(criticRating, userRating, criticRatingCount, userRatingCount);

                string releaseDateStr = ParseItem(ref html, @"<span class=""data"" itemprop=""startDate"">", @"</span>");
                DateTime releaseDate;
                if (DateTime.TryParse(releaseDateStr, out releaseDate))
                {
                    tvShow.ReleaseDate = releaseDate;
                }

                string key = UrlImagePath.Keys.FirstOrDefault(k => urlResponsePair.Url.Contains(k));
                if (key != null)
                {
                    string imgPath;
                    if (UrlImagePath.TryGetValue(key, out imgPath))
                    {
                        tvShow.ImageUrl = imgPath;
                    }
                }

                return tvShow;
            }
            else if (m_thirdLevelRequest == "details")
            {
                MediaDetail mediaDetails = new MediaDetail();

                while (html.Contains(@"<th scope=""row"">"))
                {
                    string desc = ParseItem(ref html, @"<th scope=""row"">", @":</th>");
                    string value = ParseItem(ref html, @"<td>", @"</td>");
                    if (value.Contains("</a>"))
                    {
                        value = ParseItem(ref value, @""">", @"</a>");
                    }

                    if (desc == "Seasons")
                    {
                        value = value.Replace(" ", String.Empty);
                    }

                    DetailItem detail = new DetailItem(desc, value);
                    mediaDetails.Details.Add(detail);
                }

                while (html.Contains(@"<td class=""person"">"))
                {
                    html = html.Substring(html.IndexOf(@"<td class=""person"">") +
                        @"<td class=""person"">".Length);
                    string desc = ParseItem(ref html, @""">", @"</a>");
                    string value = ParseItem(ref html, @"<td class=""role"">", @"</td>");
                    DetailItem detail = new DetailItem(desc, value);
                    mediaDetails.Details.Add(detail);
                }

                return mediaDetails;
            }

            return null;
        }

        public override void RetrieveImagePath()
        {
            UrlImagePath = m_autoResult.Where(r => Urls.Any(u => u.Contains(r.Url))).Select(r =>
                new KeyValuePair<string, string>(r.Url, r.ImagePath)).
                ToDictionary(r => r.Key, r => r.Value);
        }

        public override bool Equals(IResult obj)
        {
            bool result = false;
            if (base.Equals(obj))
            {
                result = string.Equals(Name, obj.Name, StringComparison.OrdinalIgnoreCase);
            }
            return result;
        }
    }
}
