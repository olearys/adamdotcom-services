﻿using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.ServiceModel;
using AdamDotCom.Amazon.Domain;
using AdamDotCom.Common.Service;
using AdamDotCom.Common.Service.Infrastructure;
using AdamDotCom.Common.Service.Utilities;

namespace AdamDotCom.Amazon.Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class AmazonService : IAmazon
    {
        public Reviews ReviewsByCustomerIdXml(string customerId)
        {
            return Reviews(customerId);
        }

        public Reviews ReviewsByCustomerIdJson(string customerId)
        {
            return Reviews(customerId);
        }

        public Reviews ReviewsByUsernameXml(string username)
        {
            return Reviews(DiscoverUser(username).CustomerId);
        }

        public Reviews ReviewsByUsernameJson(string username)
        {
            return Reviews(DiscoverUser(username).CustomerId);
        }

        public Wishlist WishlistByListIdXml(string listId)
        {
            return Wishlist(listId);
        }

        public Wishlist WishlistByListIdJson(string listId)
        {
            return Wishlist(listId);
        }

        public Wishlist WishlistByUsernameXml(string username)
        {
            return Wishlist(DiscoverUser(username).ListId);
        }

        public Wishlist WishlistByUsernameJson(string username)
        {
            return Wishlist(DiscoverUser(username).ListId);
        }

        public Profile DiscoverUsernameXml(string username)
        {
            return DiscoverUser(username);
        }

        public Profile DiscoverUsernameJson(string username)
        {
            return DiscoverUser(username);
        }

        private static Reviews Reviews(string customerId)
        {
            Assert.ValidInput(customerId, "customerId");

            if(ServiceCache.IsInCache<Reviews>(customerId))
            {
                var cachedReviews = (Reviews) ServiceCache.GetFromCache<Reviews>(customerId);
                if (cachedReviews != null)
                {
                    return cachedReviews;
                }
            }
            
            var amazonResponse = new AmazonFactory(BuildRequest(customerId, null)).GetResponse();

            HandleErrors(amazonResponse.Errors);

            return new Reviews(amazonResponse.Reviews.OrderByDescending(r => r.Date)).AddToCache(customerId);
        }

        private static Wishlist Wishlist(string listId)
        {
            Assert.ValidInput(listId, "listId");

            if(ServiceCache.IsInCache<Wishlist>(listId))
            {
                var cachedWishlist = (Wishlist) ServiceCache.GetFromCache<Wishlist>(listId);
                if (cachedWishlist != null)
                {
                    return cachedWishlist;
                }
            }

            var amazonResponse = new AmazonFactory(BuildRequest(null, listId)).GetResponse();

            HandleErrors(amazonResponse.Errors);

            return new Wishlist(amazonResponse.Products.OrderBy(p => p.AuthorsMLA).ThenBy(p => p.Title)).AddToCache(listId);
        }

        private static Profile DiscoverUser(string username)
        {
            username = username.Scrub();
            Assert.ValidInput(username, "username");           

            if (ServiceCache.IsInCache<Profile>(username))
            {
                var cachedProfile = (Profile) ServiceCache.GetFromCache<Profile>(username);
                if (cachedProfile != null)
                {
                    return cachedProfile;
                }
            }

            var sniffer = new ProfileSniffer(username);

            var profile = sniffer.GetProfile();
            
            HandleErrors(sniffer.Errors);

            return profile.AddToCache(username);
        }

        private static AmazonRequest BuildRequest(string customerId, string listId)
        {
            return new AmazonRequest
            {
                AssociateTag = ConfigurationManager.AppSettings["AmazonAssociateTag"],
                AccessKeyId = ConfigurationManager.AppSettings["AmazonAccessKey"],
                CustomerId = customerId,
                ListId = listId,
                SecretAccessKey = ConfigurationManager.AppSettings["AmazonSecretAccessKey"]
            };
        }

        private static void HandleErrors(List<KeyValuePair<string, string>> errors)
        {
            if(errors != null && errors.Count != 0)
            {
                throw new RestException(HttpStatusCode.BadRequest, errors, (int)ErrorCode.InternalError);
            }
        }
    }
}