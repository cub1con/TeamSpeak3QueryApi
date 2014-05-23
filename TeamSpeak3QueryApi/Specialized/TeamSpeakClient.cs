﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TeamSpeak3QueryApi.Net.Specialized
{
    public class TeamSpeakClient
    {
        private QueryClient _client;

        public TeamSpeakClient(QueryClient client)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            if (!client.IsConnected)
                throw new ArgumentException("client is not connected.");
            _client = client;
        }

        private List<Tuple<NotificationType, object, Action<NotificationData>>> _callbacks = new List<Tuple<NotificationType, object, Action<NotificationData>>>();
        public void Subscribe<T>(NotificationType notification, Action<IReadOnlyCollection<T>> callback)
            where T : Notify
        {
            Action<NotificationData> cb = (NotificationData data) =>
            {
                var specialized = NotificationDataProxy.SerializeGeneric<T>(data);
                callback(specialized);
            };

            _callbacks.Add(Tuple.Create(notification, callback as object, cb));
            _client.Subscribe(notification.ToString(), cb);
        }
        public void Unsubscribe(NotificationType notification)
        {
            var cbts = _callbacks.Where(tp => tp.Item1 == notification).ToList();
            cbts.ForEach(k => _callbacks.Remove(k));
            _client.Unsubscribe(notification.ToString());
        }
        public void Unsubscribe<T>(NotificationType notification, Action<IReadOnlyCollection<T>> callback)
            where T : Notify
        {
            var cbt = _callbacks.SingleOrDefault(t => t.Item1 == notification && t.Item2 == callback as object);
            if (cbt != null)
                _client.Unsubscribe(notification.ToString(), cbt.Item3);
        }
    }

    class NotificationDataProxy
    {
        public static IReadOnlyList<T> SerializeGeneric<T>(NotificationData data)
            where T : Notify
        {
            if (data.Payload.Count == 0)
                return new ReadOnlyCollection<T>(new T[0]);

            T destType = Activator.CreateInstance<T>();
            var pl = data.Payload;

            var fields = typeof(T).GetFields(System.Reflection.BindingFlags.Public);


            foreach (var item in pl)
            {

            }


            throw new NotImplementedException();
        }
    }

    public enum ReasonId
    {
        SwitchedChannelOrJoinedServer = 0,
        MovedItem = 1,

        Timeout = 3,
        ChannelKick = 4,
        ServerKick = 5,

        Ban = 6,
        LeftServer = 10
    }

    public enum ClientType
    {
        FullClient = 0,
        Query = 1
    }

    public abstract class Notify
    {

    }

    public class ClientEnterView : Notify
    {
        [QuerySerialize("cfid")]
        public int cfid; // (Quellchannel; „0“ beim Betreten des Servers)
        [QuerySerialize("ctid")]
        public int ctid; // (Zielchannel)
        [QuerySerialize("reasinid")]
        public ReasonId reasonid;
        [QuerySerialize("clid")]
        public int clid;
        public string client_unique_identifier;
        public string client_nickname;
        public bool client_input_muted;
        public bool client_output_muted;
        public bool client_outputonly_muted;
        public bool client_input_hardware;
        public bool client_output_hardware;
        public string client_meta_data; // (Query-Clients können hier mit clientupdate für sich selbst etwas speichern)
        public bool client_is_recording;
        public int client_database_id;
        public int client_channel_group_id;
        public string client_servergroups;// ??
        public bool client_away;
        public string client_away_message;
        public ClientType client_type; // („1“ für Query, „0“ für Voice)
        public string client_flag_avatar;// ??
        public int client_talk_power;
        public int client_talk_request;
        public string client_talk_request_msg;
        public string client_description;
        public bool client_is_talker;
        public bool client_is_priority_speaker;
        [Obsolete]
        public int client_unread_messages; // (hier immer noch vorhanden, obwohl es aus clientinfo längst gelöscht wurde)
        public string client_nickname_phonetic;
        public bool client_needed_serverquery_view_power; // (Änderung für Voice-Nutzer während einer Sitzung manchmal nicht rückwirkend, public funktioniert; jedoch bei Gruppenwechsel)
        public int client_icon_id;
        public bool client_is_channel_commander;
        public string client_country;
        public int client_channel_group_inherited_channel_id;
        public string client_badges; // (leer bei Query- und zu alten Clients, sonst in sich selbst parametrisierter String)

    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    class QuerySerializeAttribute : Attribute
    {
        public string Name { get; private set; }
        public QuerySerializeAttribute(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? null : name;
        }
    }

    // http://redeemer.biz/medien/artikel/ts3-query-notify/
    public enum NotificationType
    {
        // Server/Channel notifications
        ClientEnterView,
        ClientLeftView,

        // Server notifications
        ServerEdited,

        // Channel notifications
        ChannelDescriptionChanged,
        ChannelPasswordChanged,
        ChannelMoved,
        ChannelEdited,
        ChannelCreated,
        ChannelDeleted,
        ClientMoved,
        TextMessage,
        TokenUsed
    }
}
