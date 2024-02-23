// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Azure.Communication.Calling.Unity.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Azure.Communication.Calling.Unity
{
    public class PhotoGetter : AuthenticatedOperation
    {
        private const int httpSuccess = 200;
        private const int httpNotFound = 404;

        #region Serializable Fields
        [Header("User Settings")]

        [SerializeField]
        [Tooltip("The user id to load. If null or empty, the signed in user's load is load.")]
        private string id = null;

        /// <summary>
        /// The user id to load. If null or empty, the signed in user's load is load.
        /// </summary>
        public string Id
        {
            get => id;
            set => id = value;
        }

        [Header("Image Settings")]

        [SerializeField]
        [Tooltip("The size of the photo to load")]
        private PhotoSize photoSize = PhotoSize.Size_120x120;
        #endregion Serializable Fields

        #region Public Events
        [Header("Events")]

        [SerializeField]
        private PhotoLoadedEvent photoLoaded = new PhotoLoadedEvent();

        public event Action<PhotoGetter, PhotoLoadedEventArgs> PhotoLoaded;
        public static event Action<List<StaticUserProfile>> OnAllPhotosLoaded;
        public delegate void OnAllPhotosLoadedHanlder(List<StaticUserProfile> usersProfile);
        
        #endregion Public Events

        #region Public Properties
        public Texture2D Photo { get; private set; }
        #endregion Public Properties

        #region Protected Functions
        protected override void OnAuthenticated()
        {
            UpdateProfilesWorkerAsync();
        }
        #endregion Protected Functions

        #region Private Functions
        private async void UpdateProfilesWorkerAsync()
        {
            byte[] data = null;
            string token = Token;
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    if (string.IsNullOrEmpty(id))
                    {
                        Log.Verbose<PhotoGetter>("Requesting photo for signed in user.");
                        data = await Rest.Photo.Get(token, photoSize);
                    }
                    else
                    {
                        Log.Verbose<PhotoGetter>("Requesting photo for user ({0})", id);
                        data = await Rest.Photo.Get(token, id, photoSize);
                    }
                    Log.Verbose<PhotoGetter>("Requested for user profile completed.");
                }
                catch (Exception ex)
                {
                    Log.Error<PhotoGetter>("Failed to obtain user photo. Exception: {0}", ex);
                }
            }

            var args = new PhotoLoadedEventArgs(null);
            if (data != null)
            {
                Photo = new Texture2D(photoSize.GetWidth(), photoSize.GetHeight());
                if (Photo.LoadImage(data))
                {
                    Log.Verbose<PhotoGetter>("User photo loaded.");
                    args = new PhotoLoadedEventArgs(Photo);
                    photoLoaded?.Invoke(args);
                    PhotoLoaded?.Invoke(this, args);
                }
                else
                {
                    Log.Error<PhotoGetter>("Failed load image data into 2D texture.");
                }
            }
            else
            {
                Photo = null;
            }
            photoLoaded?.Invoke(args);
            PhotoLoaded?.Invoke(this, args);
        }
        #endregion Private Functions

        #region Public Functions
        public async void UpdateProfilesWorkerAsync(IUsers userList, OnAllPhotosLoadedHanlder handler)
        {
            string data = null;
            var deserializedData = new ReturnedPhotoData();
            string token = Token;
            var tempUserProfiles = new List<StaticUserProfile>();
            var emails = new List<string>();
            foreach(var user in userList.value)
            {
                emails.Add(user.mail);
            }  
            if (!string.IsNullOrEmpty(token))
            {
                try
                { 
                    Log.Verbose<PhotoGetter>("Requesting photos for all users");
                    data = await Rest.Photo.Get(token, emails, photoSize);
                    deserializedData = Newtonsoft.Json.JsonConvert.DeserializeObject<ReturnedPhotoData>(data);
                    Log.Verbose<PhotoGetter>("Request for user profile photos completed.");
                }
                catch (Exception ex)
                {
                    Log.Error<PhotoGetter>("Failed to obtain user photo. Exception: {0}", ex);
                }
            }
            
            if (deserializedData.responses != null && deserializedData.responses.Count() > 0)
            { 
                foreach(var response in deserializedData.responses) 
                {
                    var photo = new Texture2D(photoSize.GetWidth(), photoSize.GetHeight());
                    var count = response.id - 1;
                    var byteArr = System.Convert.FromBase64String(response.body);
                    if (response.status == httpNotFound)
                    {
                        tempUserProfiles.Add(new StaticUserProfile(userList.value[count].id, userList.value[count].displayName, userList.value[count].userPrincipalName, null, PresenceAvailability.PresenceUnknown));
                        Log.Verbose<PhotoGetter>("No user photo found.");
                    }
                    else if (response.status == httpSuccess && photo.LoadImage(byteArr))
                    {
                        tempUserProfiles.Add(new StaticUserProfile(userList.value[count].id, userList.value[count].displayName, userList.value[count].userPrincipalName, photo, PresenceAvailability.PresenceUnknown));
                        Log.Verbose<PhotoGetter>("User photo loaded."); 
                    }
                    else
                    {
                        tempUserProfiles.Add(new StaticUserProfile(userList.value[count].id, userList.value[count].displayName, userList.value[count].userPrincipalName, null, PresenceAvailability.PresenceUnknown));
                        Log.Error<PhotoGetter>("Failed load image data into 2D texture.");
                    } 
                } 
            }  
            handler?.Invoke(tempUserProfiles);
        }

        public async Task<List<StaticUserProfile>> UpdateProfilesWorkerAsync(PeopleGetter getter, PeopleChangedEventArgs args)
        {
            string data = null;
            var deserializedData = new ReturnedPhotoData();
            string token = Token;
            var tempUserProfiles = new List<StaticUserProfile>();
            var emails = new List<string>();
            foreach (var profile in getter.People)
            {
                emails.Add(profile.userPrincipalName);
            }
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    Log.Verbose<PhotoGetter>("Requesting photos for all users");
                    data = await Rest.Photo.Get(token, emails, photoSize);
                    deserializedData = Newtonsoft.Json.JsonConvert.DeserializeObject<ReturnedPhotoData>(data);
                    Log.Verbose<PhotoGetter>("Request for user profile photos completed.");
                }
                catch (Exception ex)
                {
                    Log.Error<PhotoGetter>("Failed to obtain user photo. Exception: {0}", ex);
                }
            }

            if (deserializedData.responses != null && deserializedData.responses.Count() > 0)
            {
                foreach (var response in deserializedData.responses)
                {
                    var photo = new Texture2D(photoSize.GetWidth(), photoSize.GetHeight());
                    var count = response.id - 1;
                    var byteArr = System.Convert.FromBase64String(response.body);
                    if (response.status == httpNotFound)
                    {
                        tempUserProfiles.Add(new StaticUserProfile(getter.People[count].id, getter.People[count].displayName, getter.People[count].userPrincipalName, null, PresenceAvailability.PresenceUnknown));
                        Log.Verbose<PhotoGetter>("No user photo found.");
                    }
                    else if (response.status == httpSuccess && photo.LoadImage(byteArr))
                    {
                        tempUserProfiles.Add(new StaticUserProfile(getter.People[count].id, getter.People[count].displayName, getter.People[count].userPrincipalName, photo, PresenceAvailability.PresenceUnknown));
                        Log.Verbose<PhotoGetter>("User photo loaded.");
                    }
                    else
                    {
                        tempUserProfiles.Add(new StaticUserProfile(getter.People[count].id, getter.People[count].displayName, getter.People[count].userPrincipalName, null, PresenceAvailability.PresenceUnknown));
                        Log.Error<PhotoGetter>("Failed load image data into 2D texture.");
                    }
                }
            }
            OnAllPhotosLoaded?.Invoke(tempUserProfiles);
            return tempUserProfiles;
        }
        #endregion Public Functions
    }

    [Serializable]
    public class PhotoLoadedEvent : UnityEvent<PhotoLoadedEventArgs>
    { }

    [Serializable]
    public class PhotoLoadedEventArgs
    {
        public PhotoLoadedEventArgs(Texture2D photo)
        {
            Photo = photo;
        }

        public Texture2D Photo { get; private set; }
    }

    public struct StaticUserProfile
    {
        private string m_id;
        public string Id { get { return m_id; } set => value = m_id; }
        private string m_name;
        public string DisplayName { get { return m_name; } set { m_name = value; } }
        private string m_email;
        public string Email { get { return m_email; } set { m_email = value; } }    
        private Texture2D m_icon;
        public Texture2D Icon { get { return m_icon; } set { m_icon = value; } }
        private PresenceAvailability m_Presence;
        public PresenceAvailability Presence {  get { return m_Presence; } set { m_Presence = value; } }


        public StaticUserProfile(string id, string name, string userPrincipalName, Texture2D icon, PresenceAvailability presence)
        {
            m_id = id;
            m_email = userPrincipalName;
            m_name = name;
            m_icon = icon;
            m_Presence = presence;
        } 
    }
    public struct responses
    {
        [JsonProperty(PropertyName = "id")]
        public int id { get; set; }
        [JsonProperty(PropertyName = "body")]
        public string body { get; set; }
        [JsonProperty(PropertyName = "status")]
        public int status { get; set; }
    }
    public struct ReturnedPhotoData
    {
        [JsonProperty(PropertyName = "responses")]
        public IEnumerable<responses> responses { get; set; }
    }

}
