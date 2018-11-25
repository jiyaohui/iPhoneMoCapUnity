using System;
using System.Collections;
using UnityEngine;
using UnityEditor;

public class NetworkMeshAnimator {

	private UDPServer listner;
	private SkinnedMeshRenderer meshTarget;
    private SkinnedMeshRenderer meshTargetHuayi;
    private SkinnedMeshRenderer meshTargetHuayi2;
    private UnityMainThreadDispatcher dispatcher;
	private bool isAcceptingMessages = false;

	private static NetworkMeshAnimator instance;

	public static NetworkMeshAnimator Instance
	{
		get 
		{
			if (instance == null)
			{
				instance = new NetworkMeshAnimator();
			}
			return instance;
		}
	}

	private NetworkMeshAnimator() {
		
		this.listner  = new UDPServer ((String message) => { 
			if (isAcceptingMessages) {
				dispatcher.Enqueue (SetBlendShapesOnMainThread (message));
			}
		});


		EditorApplication.playModeStateChanged += PlayStateChanged;

		listner.Start ();

	}
		
	private static void PlayStateChanged(PlayModeStateChange state)
	{
		if (state.Equals (PlayModeStateChange.ExitingPlayMode)) {
		
			instance.StopAcceptingMessages ();
		}
	}

	public void StartAcceptingMessages() {
		Debug.Log("Started accepting messages");

		meshTarget = GameObject.Find ("BlendShapeTarget").GetComponent<SkinnedMeshRenderer> ();

		if (meshTarget == null) {
			Debug.LogError ("Cannot find BlendShapeTarget. Have you added it to your scene?");
			return;
		}

		if (UnityMainThreadDispatcher.Exists()) {
			dispatcher = UnityMainThreadDispatcher.Instance ();
		} else {
			Debug.LogError ("Cannot reach BlendShapeTarget. Have you added the UnityMainThreadDispatcher to your scene?");
		}

		isAcceptingMessages = true;

	}

	public void StopAcceptingMessages() {
		Debug.Log("Stoped accepting messages");
		isAcceptingMessages = false;
	}

	public bool IsAcceptingMessages() {
		return isAcceptingMessages;
	}

	public IEnumerator SetBlendShapesOnMainThread(string messageString) {

        // 对消息字符串中以“|”分隔的每一段进行操作
		foreach (string message in messageString.Split (new Char[] { '|' }))
		{
            // 格式化接收到的消息字符串，去除空格和“msg:”字样，便于对应BlendShapes名称
            var cleanString = message.Replace (" ", "").Replace ("msg:", "");
            // 将消息字符串按分隔符“-”分拆到字符串数组（键值、权重）
			var strArray  = cleanString.Split (new Char[] {'-'});

            // 如果收到的是2个字符串（键值：权重）
			if (strArray.Length == 2) {
                // 数组中第2项为权重（浮点型）
				var weight = float.Parse (strArray.GetValue (1).ToString());

                // 数组中第2项为BlendShape名称（字符串），且将“_L”和“_R”替换为“Left”和“Right”
                var mappedShapeName = strArray.GetValue (0).ToString ().Replace ("_L", "Left").Replace ("_R", "Right");

                // 取得meshTarget（场景中的“BlendShapeTarget”）中该BlendShape名称对应的序号
                var index = meshTarget.sharedMesh.GetBlendShapeIndex (mappedShapeName);

                // 若能找到对应的序号，则将对应的BlendShape设为该序号（名称）对应的权重，否则忽略
				if (index > -1) {
					meshTarget.SetBlendShapeWeight (index, weight);
				}
			}
		}

		yield return null;
	}
}

