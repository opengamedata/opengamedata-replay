using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System;

using UnityEditor;

public class Replay : MonoBehaviour
{
    public string replayFilepath;

    public GameObject gazeObject;
    public GameObject rightHandObject;
    public GameObject leftHandObject;

    public int playbackFrame = 0;

    public List<Frame> frames = new List<Frame>();

    public bool quitOnCompletion = true;

    public bool enableSkipping = true;
    public float timeDiffToTriggerSkip = 3;

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

    //make a menu option
    [MenuItem("OpenGameData/Replay from File")]
    static void LaunchFromMenu()
    {


        string path = EditorUtility.OpenFilePanel("bin file", "", "bin");
        if (path.Length != 0)
        {
            Replay r = Selection.activeTransform.gameObject.GetComponent<Replay>();
            if (r == null)
            {
                EditorUtility.DisplayDialog("Select ReplayToVideo", "You must select a ReplayToVideo object in your project to use this function", "OK");
                return;
            }
            r.replayFilepath = path;
            //now run the application
            UnityEditor.EditorApplication.isPlaying = true;

        }
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
    // Update is called once per frame
    void Update()
    {
        //find the right frame
        while (playbackFrame < frames.Count - 2)
        {
            Frame next = frames[playbackFrame + 1];
            float playbackTime = Time.time;

            //do we need to skip ahead?
            if ((enableSkipping) && (next.time - playbackTime > timeDiffToTriggerSkip))
            {
                playbackFrame++;
            }


            if (playbackTime > next.time)
                playbackFrame++;
            else
                break;
        }

        if (playbackFrame < frames.Count - 2)
        {
            Frame frame = frames[playbackFrame];
            Frame next = frames[playbackFrame + 1];
            float playbackTime = Time.time;

            //  if (playbackTime > next.time)
            //      playbackFrame++;



            float lerp = (float)(playbackTime - frame.time) / (float)(frames[playbackFrame + 1].time - frames[playbackFrame].time);


            gazeObject.transform.position = Vector3.Lerp(frame.headPos, next.headPos, lerp);
            gazeObject.transform.rotation = Quaternion.Lerp(frame.headRot, next.headRot, lerp);

            rightHandObject.transform.position = Vector3.Lerp(frame.rightHandPos, next.rightHandPos, lerp);
            rightHandObject.transform.rotation = Quaternion.Lerp(frame.rightHandRot, next.rightHandRot, lerp);

            leftHandObject.transform.position = Vector3.Lerp(frame.leftHandPos, next.leftHandPos, lerp);
            leftHandObject.transform.rotation = Quaternion.Lerp(frame.leftHandRot, next.leftHandRot, lerp);



        }
        else if (quitOnCompletion)
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
