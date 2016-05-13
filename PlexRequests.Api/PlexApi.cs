﻿#region Copyright
// /************************************************************************
//    Copyright (c) 2016 Jamie Rees
//    File: PlexApi.cs
//    Created By: Jamie Rees
//   
//    Permission is hereby granted, free of charge, to any person obtaining
//    a copy of this software and associated documentation files (the
//    "Software"), to deal in the Software without restriction, including
//    without limitation the rights to use, copy, modify, merge, publish,
//    distribute, sublicense, and/or sell copies of the Software, and to
//    permit persons to whom the Software is furnished to do so, subject to
//    the following conditions:
//   
//    The above copyright notice and this permission notice shall be
//    included in all copies or substantial portions of the Software.
//   
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//    LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//    OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//    WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//  ************************************************************************/
using Polly;


#endregion
using System;

using NLog;

using PlexRequests.Api.Interfaces;
using PlexRequests.Api.Models.Plex;
using PlexRequests.Helpers;
using PlexRequests.Helpers.Exceptions;

using RestSharp;

namespace PlexRequests.Api
{
    public class PlexApi : IPlexApi
    {
        static PlexApi()
        {
            Version = AssemblyHelper.GetAssemblyVersion();
        }

        private static Logger Log = LogManager.GetCurrentClassLogger();
        private static string Version { get; }

        public PlexAuthentication SignIn(string username, string password)
        {
            var userModel = new PlexUserRequest
            {
                user = new UserRequest
                {
                    password = password,
                    login = username
                }
            };
            var request = new RestRequest
            {
                Method = Method.POST
            };

            AddHeaders(ref request);

            request.AddJsonBody(userModel);

            var api = new ApiRequest();

			var policy = RetryHandler.RetryAndWaitPolicy (new TimeSpan[] { 
				TimeSpan.FromSeconds (2),
				TimeSpan.FromSeconds(5),
				TimeSpan.FromSeconds(10)
			}, (exception, timespan) => Log.Error (exception, "Exception when calling SignIn for Plex, Retrying {0}", timespan));

			return (PlexAuthentication)policy.Execute(() => api.Execute<PlexAuthentication>(request, new Uri("https://plex.tv/users/sign_in.json")));
        }

        public PlexFriends GetUsers(string authToken)
        {
            var request = new RestRequest
            {
                Method = Method.GET,
            };

            AddHeaders(ref request, authToken);

            var api = new ApiRequest();
			var policy = RetryHandler.RetryAndWaitPolicy (new TimeSpan[] { 
				TimeSpan.FromSeconds (2),
				TimeSpan.FromSeconds(5),
				TimeSpan.FromSeconds(10)
			}, (exception, timespan) => Log.Error (exception, "Exception when calling GetUsers for Plex, Retrying {0}", timespan));

			var users = (PlexFriends)policy.Execute(() =>api.ExecuteXml<PlexFriends>(request, new Uri("https://plex.tv/pms/friends/all")));

            return users;
        }

        /// <summary>
        /// Gets the users.
        /// </summary>
        /// <param name="authToken">The authentication token.</param>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="plexFullHost">The full plex host.</param>
        /// <returns></returns>
        public PlexSearch SearchContent(string authToken, string searchTerm, Uri plexFullHost)
        {
            var request = new RestRequest
            {
                Method = Method.GET,
                Resource = "search?query={searchTerm}"
            };

            request.AddUrlSegment("searchTerm", searchTerm);
            AddHeaders(ref request, authToken);

            var api = new ApiRequest();
			var policy = RetryHandler.RetryAndWaitPolicy (new TimeSpan[] { 
				TimeSpan.FromSeconds (2),
				TimeSpan.FromSeconds(5),
				TimeSpan.FromSeconds(10)
			}, (exception, timespan) => Log.Error (exception, "Exception when calling SearchContent for Plex, Retrying {0}", timespan));

			var search = (PlexSearch)policy.Execute(() => api.ExecuteXml<PlexSearch>(request, plexFullHost));

            return search;
        }

        public PlexStatus GetStatus(string authToken, Uri uri)
        {
            var request = new RestRequest
            {
                Method = Method.GET,
            };

            AddHeaders(ref request, authToken);
			var policy = RetryHandler.RetryAndWaitPolicy (new TimeSpan[] { 
				TimeSpan.FromSeconds (2),
				TimeSpan.FromSeconds(5),
				TimeSpan.FromSeconds(10)
			}, (exception, timespan) => Log.Error (exception, "Exception when calling GetStatus for Plex, Retrying {0}", timespan));

            var api = new ApiRequest();
			var users = (PlexStatus)policy.Execute(() => api.ExecuteXml<PlexStatus>(request, uri));

            return users;
        }

        public PlexAccount GetAccount(string authToken)
        {
            var request = new RestRequest
            {
                Method = Method.GET,
            };

            AddHeaders(ref request, authToken);

			var policy = RetryHandler.RetryAndWaitPolicy (new TimeSpan[] { 
				TimeSpan.FromSeconds (2),
				TimeSpan.FromSeconds(5),
				TimeSpan.FromSeconds(10)
			}, (exception, timespan) => Log.Error (exception, "Exception when calling GetAccount for Plex, Retrying: {0}", timespan));


            var api = new ApiRequest();
			var account = (PlexAccount)policy.Execute(() => api.ExecuteXml<PlexAccount>(request, new Uri("https://plex.tv/users/account")));

            return account;
        }

        public PlexLibraries GetLibrarySections(string authToken, Uri plexFullHost)
        {
            var request = new RestRequest
            {
                Method = Method.GET,
                Resource = "library/sections"
            };

            AddHeaders(ref request, authToken);

            var api = new ApiRequest();
            try
            {
				var policy = RetryHandler.RetryAndWaitPolicy (new TimeSpan[] { 
					TimeSpan.FromSeconds (5),
					TimeSpan.FromSeconds(10),
					TimeSpan.FromSeconds(30)
				}, (exception, timespan) => Log.Error (exception, "Exception when calling GetLibrarySections for Plex, Retrying {0}", timespan));

				return (PlexLibraries)policy.Execute(() => api.ExecuteXml<PlexLibraries>(request, plexFullHost));
            }
			catch (Exception e)
            {
                Log.Error(e,"There has been a API Exception when attempting to get the Plex Libraries");
                return new PlexLibraries();
            }
        }

        public PlexSearch GetLibrary(string authToken, Uri plexFullHost, string libraryId)
        {
            var request = new RestRequest
            {
                Method = Method.GET,
                Resource = "library/sections/{libraryId}/all"
            };

            request.AddUrlSegment("libraryId", libraryId);
            AddHeaders(ref request, authToken);

            var api = new ApiRequest();
            try
            {
				var policy = RetryHandler.RetryAndWaitPolicy (new TimeSpan[] { 
					TimeSpan.FromSeconds (5),
					TimeSpan.FromSeconds(10),
					TimeSpan.FromSeconds(30)
				}, (exception, timespan) => Log.Error (exception, "Exception when calling GetLibrary for Plex, Retrying {0}", timespan));

				return (PlexSearch)policy.Execute(() => api.ExecuteXml<PlexSearch>(request, plexFullHost));
            }
			catch (Exception e)
            {
                Log.Error(e,"There has been a API Exception when attempting to get the Plex Library");
                return new PlexSearch();
            }
        }

        private void AddHeaders(ref RestRequest request, string authToken)
        {
            request.AddHeader("X-Plex-Token", authToken);
            AddHeaders(ref request);
        }

        private void AddHeaders(ref RestRequest request)
        {
            request.AddHeader("X-Plex-Client-Identifier", "Test213");
            request.AddHeader("X-Plex-Product", "Request Plex");
            request.AddHeader("X-Plex-Version", Version);
            request.AddHeader("Content-Type", "application/xml");
        }
    }
}

