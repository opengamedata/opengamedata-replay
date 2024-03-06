using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System;

using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;

using UnityEditor;

public class ReplayToVideo : MonoBehaviour
{
    public string replayFilepath;

    public GameObject gazeObject;
    public GameObject rightHandObject;
    public GameObject leftHandObject;

    public int playbackFrame = 0;

    public List<Frame> frames = new List<Frame>();


    RecorderController m_RecorderController;
    public bool m_RecordAudio = true;
    internal MovieRecorderSettings m_Settings = null;

    public bool quitOnCompletion = true;

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

    public FileInfo OutputFile
    {
        get
        {
            var fileName = m_Settings.OutputFile + ".mp4";
            return new FileInfo(fileName);
        }
    }




    //make a menu option
    [MenuItem("OpenGameData/Create Video From Replay")]
    static void LaunchFromMenu()
    {


        string path = EditorUtility.OpenFilePanel("bin file", "", "bin");
        if (path.Length != 0)
        {
            ReplayToVideo r2v = Selection.activeTransform.gameObject.GetComponent<ReplayToVideo>();
            if (r2v == null)
            {
                EditorUtility.DisplayDialog("Select ReplayToVideo", "You must select a ReplayToVideo object in your project to use this function", "OK");
                return;
            }
            r2v.replayFilepath = path;
            //now run the application
            UnityEditor.EditorApplication.isPlaying = true;

        }
    }

    public int repalyFolderFileCounter = 0;
    public string[] replayFolderFiles;
    [MenuItem("OpenGameData/Create Videos From Folder of Replays")]
    static void InterateOnFolderMenu()
    {
        ReplayToVideo r2v = Selection.activeTransform.gameObject.GetComponent<ReplayToVideo>();
        if (r2v == null)
        {
            EditorUtility.DisplayDialog("Select ReplayToVideo", "You must select a ReplayToVideo object in your project to use this function", "OK");
            return;
        }

        string path = EditorUtility.OpenFolderPanel("Choose folder of replay files", "", "");
        r2v.replayFolderFiles = Directory.GetFiles(path);

        //make sure we have a reply object selected


        //reset the counter
        r2v.repalyFolderFileCounter = 0;

        //this will loop things in editor
        EditorApplication.update += IterateOnFolders;

    }

    //************************************************
    //
    // Function to loop through files in editor
    //
    //************************************************
    static void IterateOnFolders()
    {
        //if we aren't playing
        if (!UnityEditor.EditorApplication.isPlaying)
        {
            ReplayToVideo r2v = Selection.activeTransform.gameObject.GetComponent<ReplayToVideo>();
            if (r2v == null)
            {
                //if we don't have an object selected, unregister
                EditorApplication.update -= IterateOnFolders;
                r2v.ClearFolder();
            }
            else
            {
                string file = r2v.replayFolderFiles[r2v.repalyFolderFileCounter];
                //make sure it is the right file type
                if (file.EndsWith(".bin"))
                {
                    Debug.Log("Create replay for " + file);
                    r2v.replayFilepath = file;
                    //now run the application
                    UnityEditor.EditorApplication.isPlaying = true;
                }

                //increment the counter
                r2v.repalyFolderFileCounter++;

                Debug.Log("Replay Counter: " + r2v.repalyFolderFileCounter + " / " + r2v.replayFolderFiles.Length);

                //have we gone through all of the files?
                if (r2v.repalyFolderFileCounter >= r2v.replayFolderFiles.Length)
                {
                    //we are done. Exit this process
                    EditorApplication.update -= IterateOnFolders;
                    r2v.ClearFolder();
                }
            }
        }
    }

    void ClearFolder()
    {
        repalyFolderFileCounter = 0;
        replayFolderFiles = new string[] { };
    }

    void CreateVideo(string filename)
    {
        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        m_RecorderController = new RecorderController(controllerSettings);

        var mediaOutputFolder = new DirectoryInfo(Path.Combine(Application.dataPath, "..", "SampleRecordings"));

        // Video
        m_Settings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        m_Settings.name = "My Video Recorder";
        m_Settings.Enabled = true;

        // This example performs an MP4 recording
        m_Settings.EncoderSettings = new CoreEncoderSettings
        {
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            Codec = CoreEncoderSettings.OutputCodec.MP4
        };
        m_Settings.CaptureAlpha = true;

        m_Settings.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1920,
            OutputHeight = 1080
        };

        // Simple file name (no wildcards) so that FileInfo constructor works in OutputFile getter.
        m_Settings.OutputFile = filename;// "/" + "video";

        // Setup Recording
        controllerSettings.AddRecorderSettings(m_Settings);
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 60.0f;

        RecorderOptions.VerboseMode = false;
        m_RecorderController.PrepareRecording();
        m_RecorderController.StartRecording();

        Debug.Log($"Started recording for file {OutputFile.FullName}");
    }

    void OnDisable()
    {
        m_RecorderController.StopRecording();
      
    }

    // Start is called before the first frame update
    void Start()
    {
        BinaryReader binaryReader = new(File.Open(replayFilepath, FileMode.Open));
        if (binaryReader == null)
            return;

        string videoPath = Path.Combine(Path.GetDirectoryName(replayFilepath), Path.GetFileNameWithoutExtension(replayFilepath));


        CreateVideo(videoPath);

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

        //are we looping over a folder?
      //  Debug.Log("Test Replay Counter: " + repalyFolderFileCounter + " / " + replayFolderFiles.Length);
        if (repalyFolderFileCounter < replayFolderFiles.Length)
        {
            //we are done. Exit this process
            EditorApplication.update += IterateOnFolders;
          //  Debug.Log("register replay callback");
        }


        //register the quit function
        Application.quitting += Quit;

    }

    // Update is called once per frame
    void Update()
    {
        //find the right frame
        while (playbackFrame < frames.Count - 2)
        {
            Frame next = frames[playbackFrame + 1];
            float playbackTime = Time.time;

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

    static void Quit()
    {
        ReplayToVideo r2v = Selection.activeTransform.gameObject.GetComponent<ReplayToVideo>();
        if (r2v == null)
        {
           
        }

            
    }



 
    
}
