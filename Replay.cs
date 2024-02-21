using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System;

public class Replay : MonoBehaviour
{
    public string replayFilepath;

    public GameObject gazeObject;
    public GameObject rightHandObject;
    public GameObject leftHandObject;

    public int playbackFrame = 0;

    public List<Frame> frames = new List<Frame>();

    [Serializable]
    public class Frame
    {
        public float time;
        public Vector3 headPos;
        public Quaternion headRot;
        public Vector3 rightHandPos;
        public Quaternion rightHandRot;
        public Vector3 leftHandPos;
        public Quaternion leftHandRot;
    }

    // Start is called before the first frame update
    void Start()
    {
        BinaryReader binaryReader = new(File.Open(replayFilepath, FileMode.Open));
        if (binaryReader == null)
            return;

        while (binaryReader.BaseStream.Position != binaryReader.BaseStream.Length)
        {
            Frame frame = new Frame();
            frame.time = binaryReader.ReadSingle();
            for (int ii = 0; ii < 3; ii++)
                frame.headPos[ii] = binaryReader.ReadSingle();
            for (int ii = 0; ii < 4; ii++)
                frame.headRot[ii] = binaryReader.ReadSingle();
            for (int ii = 0; ii < 3; ii++)
                frame.rightHandPos[ii] = binaryReader.ReadSingle();
            for (int ii = 0; ii < 4; ii++)
                frame.rightHandRot[ii] = binaryReader.ReadSingle();
            for (int ii = 0; ii < 3; ii++)
                frame.leftHandPos[ii] = binaryReader.ReadSingle();
            for (int ii = 0; ii < 4; ii++)
                frame.leftHandRot[ii] = binaryReader.ReadSingle();

            frames.Add(frame);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (playbackFrame < frames.Count + 1)
        {
            Frame frame = frames[playbackFrame];
            Frame next = frames[playbackFrame + 1];
            float playbackTime = Time.time;

            if (playbackTime > next.time)
                playbackFrame++;

            
                
            float lerp = (float)(playbackTime - frame.time) / (float)(frames[playbackFrame + 1].time - frames[playbackFrame].time);


            gazeObject.transform.position = Vector3.Lerp(frame.headPos, next.headPos, lerp);
            gazeObject.transform.rotation = Quaternion.Lerp(frame.headRot, next.headRot, lerp);

            rightHandObject.transform.position = Vector3.Lerp(frame.rightHandPos, next.rightHandPos, lerp);
            rightHandObject.transform.rotation = Quaternion.Lerp(frame.rightHandRot, next.rightHandRot, lerp);

            leftHandObject.transform.position = Vector3.Lerp(frame.leftHandPos, next.leftHandPos, lerp);
            leftHandObject.transform.rotation = Quaternion.Lerp(frame.leftHandRot, next.leftHandRot, lerp);


                   
        }
    }
}
