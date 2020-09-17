using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Unigine;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Console = System.Console;

public static class ServerLogic
{
    private static readonly Mutex Mutex = new Mutex();

    private static class CurrentState
    {
        // public static string ClientId;
        public static int NodeId = -1;
    }

    [Serializable]
    public class EventMessage
    {
        [JsonProperty("event")] public string Event { get; set; }

        [JsonProperty("data")] public object Data { get; set; }
    }

    [Serializable]
    public class ResponseSubscribe
    {
        [JsonProperty("event")] public string Event = "subscribe";

        [JsonProperty("nodes")] public List<NodeDataSmall> NodesData;

        public ResponseSubscribe(List<NodeDataSmall> nodesData)
        {
            NodesData = nodesData;
        }
    }

    [Serializable]
    struct MessageSubscribeNode
    {
        // public string ClientId;
        public string NodeId;

        public MessageSubscribeNode(string nodeId)
        {
            NodeId = nodeId;
        }
    }

    [Serializable]
    struct MessageUpdateProperty
    {
        [field: JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }

        [field: JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [field: JsonProperty(PropertyName = "value")]
        public string Value { get; set; }

        public MessageUpdateProperty(string nodeId, string name, string value)
        {
            NodeId = nodeId;
            Name = name;
            Value = value;
        }
    }

    [Serializable]
    public struct NodeDataSmall
    {
        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("id")] public string Id { get; set; }

        [JsonProperty("parentId")] public string ParentId { get; set; }

        public NodeDataSmall(string name, string id, string parentId)
        {
            Name = name;
            Id = id;
            ParentId = parentId;
        }
    }

    [Serializable]
    class MessageNodeData
    {
        [JsonProperty("event")] public string Event = "subscribe_node";

        [JsonProperty("components")] public List<InspectorTypes.ComponentData> Components;

        public MessageNodeData(List<InspectorTypes.ComponentData> fields)
        {
            Components = fields;
        }
    }

    [Serializable]
    class ResponsePropertyUpdate
    {
        [JsonProperty("event")] public string Event = "update_property";

        [JsonProperty("updates")] public List<InspectorTypes.FieldValue> Updates;

        public ResponsePropertyUpdate(List<InspectorTypes.FieldValue> updates)
        {
            Updates = updates;
        }
    }

    public static async Task JoinToLocalServer()
    {
        await Start();
    }

    private static NetworkStream _stream2;

    private static async Task Start()
    {
        const string ip = "127.0.0.1";
        const int port = 9090;
        var server = new TcpListener(IPAddress.Parse(ip), port);

        var callback = new TimerCallback(NotifyAboutChanges);
        var timer = new Timer(callback, 0, 0, 100);

        server.Start();
        Console.WriteLine("Server has started on {0}:{1}, Waiting for a connection...", ip, port);

        var client = server.AcceptTcpClient();
        Console.WriteLine("A client connected.");

        var stream = client.GetStream();
        _stream2 = stream;

        // enter to an infinite cycle to be able to handle every change in stream
        while (true)
        {
            while (!stream.DataAvailable)
            {
            }

            // match against "get"
            while (client.Available < 3)
            {
            }

            var bytes = new byte[client.Available];
            stream.Read(bytes, 0, client.Available);
            var s = Encoding.UTF8.GetString(bytes);

            if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
            {
                Console.WriteLine("=====Handshaking from client=====\n{0}", s);

                // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                // 3. Compute SHA-1 and Base64 hash of the new value
                // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                var swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                var swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                var swkaSha1 = System.Security.Cryptography.SHA1.Create()
                    .ComputeHash(Encoding.UTF8.GetBytes(swka));
                var swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                var response = Encoding.UTF8.GetBytes(
                    "HTTP/1.1 101 Switching Protocols\r\n" +
                    "Connection: Upgrade\r\n" +
                    "Upgrade: websocket\r\n" +
                    "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                stream.Write(response, 0, response.Length);
            }
            else
            {
                bool fin = (bytes[0] & 0b10000000) != 0,
                    mask = (bytes[1] & 0b10000000) !=
                           0; // must be true, "All messages from the client to the server have this bit set"

                int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                    msglen = bytes[1] - 128, // & 0111 1111
                    offset = 2;

                if (msglen == 126)
                {
                    // was ToUInt16(bytes, offset) but the result is incorrect
                    msglen = BitConverter.ToUInt16(new byte[] {bytes[3], bytes[2]}, 0);
                    offset = 4;
                }
                else if (msglen == 127)
                {
                    Console.WriteLine("TODO: msglen == 127, needs qword to store msglen");
                    // i don't really know the byte order, please edit this
                    // msglen = BitConverter.ToUInt64(new byte[] { bytes[5], bytes[4], bytes[3], bytes[2], bytes[9], bytes[8], bytes[7], bytes[6] }, 0);
                    // offset = 10;
                }

                if (msglen == 0)
                    Console.WriteLine("msglen == 0");
                else if (mask)
                {
                    var decoded = new byte[msglen];
                    var masks = new byte[4]
                        {bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3]};
                    offset += 4;

                    for (var i = 0; i < msglen; ++i)
                        decoded[i] = (byte) (bytes[offset + i] ^ masks[i % 4]);

                    var text = Encoding.UTF8.GetString(decoded);
                    Console.WriteLine("{0}", text);
                    HandleMessage(stream, text);
                }
                else
                    Console.WriteLine("mask bit not set");

                Console.WriteLine();
            }
        }
    }

    private static void HandleMessage(NetworkStream stream, string text)
    {
        // Crazy shit around JSON.stringify or something other HELP ME PLEASE
        var elems = text.Trim('"').Split('\\');
        var message = JsonConvert.DeserializeObject<EventMessage>(string.Join("", elems));
        switch (message.Event)
        {
            case "subscribe":
                HandleSubscribe(stream);
                break;
            case "update_property":
            {
                var deserializeObject =
                    JsonConvert.DeserializeObject<MessageUpdateProperty>(message.Data.ToString());
                HandleUpdate(deserializeObject);
            }
                break;
            case "subscribe_node":
            {
                HandleNode(stream, message.Data);
            }
                break;
            default:
                Console.WriteLine($"HandleMessage() not handled event: {message.Event}");
                break;
        }
    }

    private static void HandleSubscribe(NetworkStream stream)
    {
        // var serializeObject = JsonConvert.SerializeObject(_data);
        var message = new ResponseSubscribe(GetAllComponents());
        var objectStr = JsonConvert.SerializeObject(message);
        SendObject(stream, objectStr);
    }

    private static void HandleNode(NetworkStream stream, object deserializedObject)
    {
        var subscribeNode = JsonConvert.DeserializeObject<MessageSubscribeNode>(deserializedObject.ToString());
        UnsubscribeFromInspected();

        CurrentState.NodeId = Int32.Parse(subscribeNode.NodeId);

        var node = World.GetNodeById(CurrentState.NodeId);
        var components = node.GetComponents<BasicComponent>();

        var nodeComponents = new List<InspectorTypes.ComponentData>();
        foreach (var component in components)
        {
            nodeComponents.Add(component.GetComponentStructure());
            component.Subscribe();
        }

        var message = JsonConvert.SerializeObject(new MessageNodeData(nodeComponents));
        SendObject(stream, message);
    }

    private static void UnsubscribeFromInspected()
    {
        if (CurrentState.NodeId == -1) return;
        var lastNode = World.GetNodeById(CurrentState.NodeId);
        var components = lastNode.GetComponents<BasicComponent>();
        foreach (var component in components)
        {
            component.Unsubscribe();
        }
    }

    private static List<NodeDataSmall> GetAllComponents()
    {
        ICollection<Node> nodes = new List<Node>();
        List<NodeDataSmall> nodePackages = new List<NodeDataSmall>();
        World.GetNodes(nodes);
        foreach (var node in nodes)
        {
            var parentId = "-1";
            if (node.Parent != null)
            {
                parentId = node.Parent.ID.ToString();
            }

            nodePackages.Add(new NodeDataSmall(node.Name, node.ID.ToString(), parentId));
        }

        return nodePackages;
    }

    private static void HandleUpdate(MessageUpdateProperty messageUpdateProperty)
    {
        var node = World.GetNodeById(Int32.Parse(messageUpdateProperty.NodeId));
        var component = node.GetComponent<Component>();
        var fields = component.GetType().GetFields();
        foreach (var field in fields)
        {
            if (field.Name.Equals(messageUpdateProperty.Name))
            {
                switch (field.FieldType.Name)
                {
                    case "String":
                        field.SetValue(component, messageUpdateProperty.Value);
                        Console.WriteLine("Updated as String");
                        break;
                    case "Single":
                        field.SetValue(component, Single.Parse(messageUpdateProperty.Value));
                        Console.WriteLine("Updated as Float");
                        break;
                    case "Int32":
                        field.SetValue(component, Int32.Parse(messageUpdateProperty.Value));
                        Console.WriteLine("Updated as Integer");
                        break;
                    default:
                        Console.WriteLine($"Got update for: {field.FieldType.Name} but no handled");
                        break;
                }
            }
        }
    }

    // Return true if notify sent
    private static void NotifyAboutChanges(object state)
    {
        Console.WriteLine($"Updates: {_propertyChanges.Count}");

        if (_propertyChanges.Count == 0) return;
        if (_stream2 == null) return;

        Mutex.WaitOne();
        var message = JsonConvert.SerializeObject(new ResponsePropertyUpdate(_propertyChanges));
        SendObject(_stream2, message);
        _propertyChanges.Clear();
        Mutex.ReleaseMutex();
    }

    private static bool SendObject(NetworkStream stream, string objectStr)
    {
        var encodedMessage = EncodeMessageToSend(objectStr);
        stream.Write(encodedMessage, 0, encodedMessage.Length);
        return true;
    }

    private static byte[] EncodeMessageToSend(string message)
    {
        byte[] bytesRaw = Encoding.UTF8.GetBytes(message);
        byte[] frame = new byte[10];

        Int32 indexStartRawData = -1;
        Int32 length = bytesRaw.Length;

        frame[0] = (byte) 129;
        if (length <= 125)
        {
            frame[1] = (byte) length;
            indexStartRawData = 2;
        }
        else if (length >= 126 && length <= 65535)
        {
            frame[1] = (byte) 126;
            frame[2] = (byte) ((length >> 8) & 255);
            frame[3] = (byte) (length & 255);
            indexStartRawData = 4;
        }
        else
        {
            frame[1] = (byte) 127;
            frame[2] = (byte) ((length >> 56) & 255);
            frame[3] = (byte) ((length >> 48) & 255);
            frame[4] = (byte) ((length >> 40) & 255);
            frame[5] = (byte) ((length >> 32) & 255);
            frame[6] = (byte) ((length >> 24) & 255);
            frame[7] = (byte) ((length >> 16) & 255);
            frame[8] = (byte) ((length >> 8) & 255);
            frame[9] = (byte) (length & 255);

            indexStartRawData = 10;
        }

        var response = new byte[indexStartRawData + length];

        int i, reponseIdx = 0;

        //Add the frame bytes to the reponse
        for (i = 0; i < indexStartRawData; i++)
        {
            response[reponseIdx] = frame[i];
            reponseIdx++;
        }

        //Add the data bytes to the response
        for (i = 0; i < length; i++)
        {
            response[reponseIdx] = bytesRaw[i];
            reponseIdx++;
        }

        return response;
    }

    public static void AddChanges(InspectorTypes.FieldValue field)
    {
        Mutex.WaitOne();
        var find = _propertyChanges.Find(value => value.Field == field.Field);
        if (find.Field != null)
        {
            _propertyChanges.Remove(find);
            _propertyChanges.Add(field);
        }
        else
        {
            _propertyChanges.Add(field);
        }

        Mutex.ReleaseMutex();
    }

    private static List<InspectorTypes.FieldValue> _propertyChanges = new List<InspectorTypes.FieldValue>();
}