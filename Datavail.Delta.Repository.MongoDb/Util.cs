﻿using System;
using System.Configuration;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure;
using Datavail.Delta.Infrastructure.Repository;
using MongoDB.Driver;

namespace Datavail.Delta.Repository.MongoDb
{
    /// <summary>
    /// Internal miscellaneous utility functions.
    /// </summary>
    internal static class Util
    {
        /// <summary>
        /// The default key MongoRepository will look for in the App.config or Web.config file.
        /// </summary>
        private const string DEFAULT_CONNECTIONSTRING_NAME = "MongoServerSettings";

        /// <summary>
        /// Retrieves the default connectionstring from the App.config or Web.config file.
        /// </summary>
        /// <returns>Returns the default connectionstring from the App.config or Web.config file.</returns>
        public static string GetDefaultConnectionString()
        {
            return ConfigurationManager.ConnectionStrings[DEFAULT_CONNECTIONSTRING_NAME].ConnectionString;
        }

        /// <summary>
        /// Creates and returns a MongoDatabase from the specified url.
        /// </summary>
        /// <param name="url">The url to use to get the database from.</param>
        /// <returns>Returns a MongoDatabase from the specified url.</returns>
        private static MongoDatabase GetDatabaseFromUrl(MongoUrl url)
        {
            var server = MongoServer.Create(url.ToServerSettings());
            return server.GetDatabase(url.DatabaseName);
        }

        /// <summary>
        /// Creates and returns a MongoCollection from the specified type and connectionstring.
        /// </summary>
        /// <typeparam name="T">The type to get the collection of.</typeparam>
        /// <param name="connectionstring">The connectionstring to use to get the collection from.</param>
        /// <returns>Returns a MongoCollection from the specified type and connectionstring.</returns>
        public static MongoCollection<T> GetCollectionFromConnectionString<T>(string connectionstring)
            where T : IEntity
        {
            return GetDatabaseFromUrl(new MongoUrl(connectionstring))
                .GetCollection<T>(GetCollectionName<T>());
        }

        /// <summary>
        /// Creates and returns a MongoCollection from the specified type and url.
        /// </summary>
        /// <typeparam name="T">The type to get the collection of.</typeparam>
        /// <param name="url">The url to use to get the collection from.</param>
        /// <returns>Returns a MongoCollection from the specified type and url.</returns>
        public static MongoCollection<T> GetCollectionFromUrl<T>(MongoUrl url)
            where T : IEntity
        {
            return Util.GetDatabaseFromUrl(url)
                .GetCollection<T>(GetCollectionName<T>());
        }

        /// <summary>
        /// Determines the collectionname for T and assures it is not empty
        /// </summary>
        /// <typeparam name="T">The type to determine the collectionname for.</typeparam>
        /// <returns>Returns the collectionname for T.</returns>
        private static string GetCollectionName<T>() where T : IEntity
        {
            string collectionName;
            var baseType = typeof (T).BaseType;
            if (baseType != null && baseType == typeof(object))
            {
                collectionName = GetCollectioNameFromInterface<T>();
            }
            else
            {
                collectionName = GetCollectionNameFromType(typeof(T));
            }

            if (string.IsNullOrEmpty(collectionName))
            {
                throw new ArgumentException("Collection name cannot be empty for this entity");
            }
            return collectionName;
        }



        /// <summary>
        /// Determines the collectionname from the specified type.
        /// </summary>
        /// <typeparam name="T">The type to get the collectionname from.</typeparam>
        /// <returns>Returns the collectionname from the specified type.</returns>
        private static string GetCollectioNameFromInterface<T>()
        {
            string collectionname;

            // Check to see if the object (inherited from Entity) has a MongoCollectionName attribute
            var att = Attribute.GetCustomAttribute(typeof(T), typeof(MongoCollectionName));
            if (att != null)
            {
                // It does! Return the value specified by the MongoCollectionName attribute
                collectionname = ((MongoCollectionName)att).Name;
            }
            else
            {
                collectionname = typeof(T).Name;
            }

            return collectionname;
        }

        /// <summary>
        /// Determines the collectionname from the specified type.
        /// </summary>
        /// <param name="entitytype">The type of the entity to get the collectionname from.</param>
        /// <returns>Returns the collectionname from the specified type.</returns>
        private static string GetCollectionNameFromType(Type entitytype)
        {
            string collectionname;

            // Check to see if the object (inherited from Entity) has a MongoCollectionName attribute
            var att = Attribute.GetCustomAttribute(entitytype, typeof(MongoCollectionName));
            if (att != null)
            {
                // It does! Return the value specified by the MongoCollectionName attribute
                collectionname = ((MongoCollectionName)att).Name;
            }
            else
            {
                // No attribute found, get the basetype
                while (entitytype.BaseType != null && !(entitytype.BaseType == typeof(DomainBase)))
                {
                    entitytype = entitytype.BaseType;
                }

                collectionname = entitytype.Name;
            }

            return collectionname;
        }
    }
}
