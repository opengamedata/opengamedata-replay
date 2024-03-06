using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using UnityEditor;

using System;


public class Parser : MonoBehaviour
{
    public string DataDirectory;
    public List<string> FilterSessions;
    
    public GameObject sessionObjectTemplate;
    public List<GameObject> sessionObjects;

    static public Hashtable sessionHash = new Hashtable();
    static public List<Session> sessions = new List<Session>();

    [Serializable]
    public class GameState
    {
        public float posX;
        public float posY;
        public float posZ;
        public float rotW;
        public float rotX;
        public float rotY;
        public float rotZ;
        public float seconds_from_launch;
    };



    public class Session
    {
        public string id;

        public List<Packet> packetList = new List<Packet>();
        public float packetTime = 0;
        public float deltaTime = 0;

        public List<Frame> gazeFrames = new List<Frame>();
        public List<Frame> leftHandFrames = new List<Frame>();
        public List<Frame> rightHandFrames = new List<Frame>();



        [Serializable]
        public class Frame
        {
            public float time;
            public Vector3 pos;
            public Quaternion rot;
        };


        [Serializable]
        public class Packet
        {
            public float time;
            public string type;
            public string data;
        };

        public Session(string id)
        {
            this.id = id;
        }


        public void AddGazeFrame(Frame frame)
        {

            gazeFrames.Add(frame);
        }

        public void AddLeftHandFrame(Frame frame)
        {

            leftHandFrames.Add(frame);
        }

        public void AddRightHandFrame(Frame frame)
        {

            rightHandFrames.Add(frame);
        }



        public void addPacket(float time, string type, string data)
        {
            //first let's make it
            Packet packet = new Packet();
            packet.time = time;
            packet.type = type;
            packet.data = data;

            //is this the first?
            if (packetList.Count == 0)
            {
                packetList.Add(packet);
                return;
            }

            //is this at the back?
            Packet checkPacket = packetList[packetList.Count - 1];

            if (checkPacket.time <= packet.time)
            {
                packetList.Add(packet);
            }
            else
            {
                // Debug.LogError("packet out of order " + last.time + " > " + packet.time);

                //find the right place to add this
                for (int i = packetList.Count - 2; i >= 0; i--)
                {
                    checkPacket = packetList[i];
                    if (checkPacket.time <= packet.time)
                    {
                        packetList.Insert(i + 1, packet);
                        return;
                    }
                }
                //we never found a spot so add it at zero
                packetList.Insert(0, packet);
            }
        }

        public void setPacketTime(float time)
        {
            if (time == packetTime)
                return;
            else if (time < packetTime)
                Debug.LogError("Out of order packets " + time + " < " + packetTime);


            deltaTime = time - packetTime;
            packetTime = time;
        }

        public float sequenceStartTime()
        {
            return packetTime - deltaTime;
        }

        public float sequenceEndTime()
        {
            return packetTime;
        }

        public void WriteBinaryFile(string filename)
        {
            BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Create));
            for (int i = 0; i < gazeFrames.Count; i++)
            {
                //lets write our values
                writer.Write(gazeFrames[i].time);
                for (int ii = 0; ii < 3; ii++)
                    writer.Write(gazeFrames[i].pos[ii]);
                for (int ii = 0; ii < 4; ii++)
                    writer.Write(gazeFrames[i].rot[ii]);
                for (int ii = 0; ii < 3; ii++)
                    writer.Write(rightHandFrames[i].pos[ii]);
                for (int ii = 0; ii < 4; ii++)
                    writer.Write(rightHandFrames[i].rot[ii]);
                for (int ii = 0; ii < 3; ii++)
                    writer.Write(leftHandFrames[i].pos[ii]);
                for (int ii = 0; ii < 4; ii++)
                    writer.Write(leftHandFrames[i].rot[ii]);
            }
            writer.Close();
        }

        public string CSVStats(bool writeHeader=false)
        {
            string line= "";

            line += writeHeader ? "id" : id;
            line += ",";
            line += writeHeader ? "frameCount" : gazeFrames.Count;
            line += ",";
            line += writeHeader ? "duration" : gazeFrames[gazeFrames.Count-1].time;

            return line;

        }
    }

    [MenuItem("OpenGameData/Parse Log File to Binary")]
    static void ParseMenu()
    {
        //lets clear out sessions
        foreach (Session session in sessions)
        {
            //DestroyImmediate(session);

        }
        sessions.Clear();
        sessionHash.Clear();

        string path = EditorUtility.OpenFilePanel("og file", "", "tsv");
        if (path.Length != 0)
        {
            Parse(path);

        }
        Debug.Log("Found " + sessions.Count + " Sessions");
        //TODO have this based on user input
        string directory = Path.GetDirectoryName(path);
        foreach (Session session in sessions)
        {
            foreach (Session.Packet packet in session.packetList)
            {
                session.setPacketTime(packet.time);
                ParsePackage(packet.type, packet.data, session);
            }
            session.WriteBinaryFile(directory + "/" + session.id + ".bin");
        }

        //write the stats
        StreamWriter writer = new StreamWriter(directory + "/" + "stats.csv");
        //write the header
        writer.WriteLine(sessions[0].CSVStats(true));
        //write the info
        foreach (Session session in sessions)
        {
            writer.WriteLine(session.CSVStats());
        }
        writer.Close();
    }

    // Start is called before the first frame update
    void Start()
    {

        ParseFilesFromDir(DataDirectory);

        //close out all of the hash

        for (int i = 0; i < transform.childCount; i++)
        {
            // Debug.Log(transform.GetChild(i).gameObject.name);
            Session session = transform.GetChild(i).gameObject.GetComponent<Session>();

            foreach(Session.Packet packet in session.packetList)
            {
                session.setPacketTime(packet.time);
                ParsePackage(packet.type, packet.data, session);
            }
            // session.setPacketTime(gs.seconds_from_launch);

            // Debug.Log(gs.seconds_from_launch);

            //Debug.Log(items[event_data_index]);
            //ParsePackage(items[event_data_index], session);

         //   session.Finish();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    

    //header stuff we care about
    static int session_id_index = -1;
    static int event_name_index = -1;
    static int event_data_index = -1;
    static int game_state_index = -1;


    public void ParseFilesFromDir(string folderPath)
    {
        Debug.Log("parse folder: " + folderPath);
        foreach (string file in Directory.EnumerateFiles(folderPath, "*.tsv"))
        {

            Parse(file);
        }
    }

    static void SetHeaders(string line)
    {
        char[] seperators = { '\t' };
        string[] items = line.Split(seperators);

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == "session_id")
                session_id_index = i;
            else if (items[i] == "event_name")
                event_name_index = i;
            else if (items[i] == "event_data")
                event_data_index = i;
            else if (items[i] == "game_state")
                game_state_index = i;
        }
    }

    static Session getSession(string id)
    {
        Session session;
        if (sessionHash.ContainsKey(id))
        {
            session = (Session)sessionHash[id];
        }
        else
        {
            session = new Session(id.Trim('"'));
            //add it to the list
            sessions.Add(session);
            //add it to the hash
            sessionHash.Add(id, session);

        }
        return session;
    }

    static void CreateGazeSequence(string type, float startTime, float endTime, Session session, List<Vector3> positions, List<Quaternion> rotations)
    {
        float timeStep = (endTime - startTime) / (positions.Count);

        for (int i=0; i < positions.Count; i++)
        {
            float t = startTime + (i) * timeStep;
            Session.Frame frame = new Session.Frame();
            frame.time = t;
            frame.pos = positions[i];
            frame.rot = rotations[i];
            if (type == "gaze_data_package")
                session.AddGazeFrame(frame);
            else if (type == "left_hand_data_package")
                session.AddLeftHandFrame(frame);
            else if (type == "right_hand_data_package")
                session.AddRightHandFrame(frame);
        }
    }

    static void ParseLine(string line)
    {
        char[] seperators = { '\t' };
        string[] items = line.Split(seperators);


        Session session = getSession(items[session_id_index]);
 

        //check the package type
        if (items[event_name_index] == "\"viewport_data\"")
        {

            GameState gs = JsonUtility.FromJson<GameState>(items[game_state_index]);
            session.addPacket(gs.seconds_from_launch, "gaze_data_package", items[event_data_index]);

        }
        else if (items[event_name_index] == "\"left_hand_data\"")
        {

            GameState gs = JsonUtility.FromJson<GameState>(items[game_state_index]);
            session.addPacket(gs.seconds_from_launch, "left_hand_data_package", items[event_data_index]);
        }
        else if (items[event_name_index] == "\"right_hand_data\"")
        {

            GameState gs = JsonUtility.FromJson<GameState>(items[game_state_index]);
            session.addPacket(gs.seconds_from_launch, "right_hand_data_package", items[event_data_index]);
        }
    }

    static void ParsePackage(string type, string package, Session session)
    {
        char[] seperators = { '"', ':', ' ', ',', '{', '}', '\\', '[', ']', '\"' };
        string[] tokens = package.Split(seperators);
        List<string> items = new List<string>();
        for (int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] != "")
                items.Add(tokens[i]);
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == type)
            {
                //increment i
                i++;

                Vector3 pos;
                Quaternion rot;
                List<Vector3> positions = new List<Vector3>();
                List<Quaternion> rotations = new List<Quaternion>();
                for (; i < items.Count; i++)
                {
                    if (items[i] == "pos")
                    {
                        pos.x = float.Parse(items[++i]);
                        pos.y = float.Parse(items[++i]);
                        pos.z = float.Parse(items[++i]);

                        positions.Add(pos);
                      
                    }
                    else if (items[i] == "rot")
                    {
                        rot.x = float.Parse(items[++i]);
                        rot.y = float.Parse(items[++i]);
                        rot.z = float.Parse(items[++i]);
                        rot.w = float.Parse(items[++i]);

                        rotations.Add(rot);
                    }
                    else
                    {

                        //create the sequence (interpolate time)
                        CreateGazeSequence(type, session.sequenceStartTime(), session.sequenceEndTime(), session, positions, rotations);

                        return;
                    }
                }

            }
        }
    }

    public static void Parse(string fileName)
    {

        Debug.Log("Parse: " + fileName);

        using (StreamReader sr = File.OpenText(fileName))
        {
            string line;
            int lineNumber = 0;
            while ((line = sr.ReadLine()) != null)
            {


                if (lineNumber == 0)
                    SetHeaders(line);
                else
                    ParseLine(line);

                lineNumber++;

            }
        }


    }

       
}
