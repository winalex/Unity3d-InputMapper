using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ws.winx.input;
using ws.winx.devices;
using System.Runtime.Serialization;

namespace ws.winx.input{




	#if (UNITY_STANDALONE || UNITY_EDITOR || UNITY_ANDROID) && !UNITY_WEBPLAYER
	[DataContract]
	#endif
	public class InputPlayer 
	{

		public enum Player:int{
			Player0,
			Player1,
			Player2,
			Player3,
			Player4,
			Player5,
			Player6,
			Player7
			
			
			
		}

		#if (UNITY_STANDALONE || UNITY_EDITOR || UNITY_ANDROID) && !UNITY_WEBPLAYER
		[DataMember(Order=1)]
		#endif
		 Dictionary<string,Dictionary<int,InputState>> _DeviceStateInputs;


		public Dictionary<string, Dictionary<int, InputState>> DeviceProfileStateInputs {
			get {
				if(_DeviceStateInputs==null) _DeviceStateInputs=new Dictionary<string, Dictionary<int, InputState>>();
				return _DeviceStateInputs;
			}
			set {
				_DeviceStateInputs=value;

			}
		}


		public IDevice Device;


		#if (UNITY_STANDALONE || UNITY_EDITOR || UNITY_ANDROID) && !UNITY_WEBPLAYER
		[DataMember(Order=1)]
		#endif
		public string _deviceID;

		public string DeviceID {
			get {
				if(Device!=null)
					return Device.ID;
				else
					return _deviceID;
			}
			set {
				_deviceID = value;
			}
		}



		public InputPlayer Clone(){
			InputPlayer newInputPlayer = new InputPlayer ();
			Dictionary<int,InputState> stateInputs;

			foreach (var DeviceHashStateInputPair in this.DeviceProfileStateInputs) {

				stateInputs=newInputPlayer.DeviceProfileStateInputs[DeviceHashStateInputPair.Key]=new Dictionary<int,InputState>();

				foreach(var HashStateInputPair in DeviceHashStateInputPair.Value){
					stateInputs[HashStateInputPair.Key]=HashStateInputPair.Value.Clone();
				}


			}


			return newInputPlayer;

		}





	}
}

