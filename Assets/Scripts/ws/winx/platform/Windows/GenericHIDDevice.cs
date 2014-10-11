﻿#if UNITY_STANDALONE_WIN || UNITY_EDITOR
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.17929
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.Threading;
using System.Runtime.InteropServices;
using ws.winx.devices;
using System.Timers;
using System.Diagnostics;
using System.Collections.Generic;


namespace ws.winx.platform.windows
{
    public enum DeviceMode
    {
        NonOverlapped = 0,
        Overlapped = 1
    }


    public class HidAsyncState
    {
        private readonly object _callerDelegate;
        private readonly object _callbackDelegate;

        public HidAsyncState(object callerDelegate, object callbackDelegate)
        {
            _callerDelegate = callerDelegate;
            _callbackDelegate = callbackDelegate;
        }

        public object CallerDelegate { get { return _callerDelegate; } }
        public object CallbackDelegate { get { return _callbackDelegate; } }
    }


   



    public class GenericHIDDevice :  HIDDevice,IDisposable
    {


      

        protected delegate HIDReport ReadDelegate(int timeout);
        private delegate bool WriteDelegate(byte[] data,int timeout);
        
        public IntPtr ReadHandle { get; private set; }
        public IntPtr WriteHandle { get; private set; }
        public IntPtr ReadAsyncHandle { get; private set; }
        public IntPtr WriteAsyncHandle { get; private set; }


        public bool IsOpen { get; private set; }
        volatile bool IsReadInProgress = false;

   

      
        private HIDReport __lastHIDReport;


        private int _InputReportByteLength=8;

        override public int InputReportByteLength
        {
            get { return _InputReportByteLength; }
            set {
                if (value < 2) throw new Exception("InputReportByteLength should be >1 ");  _InputReportByteLength = value; }
        }
        private int _OutputReportByteLength=8;

        override public int OutputReportByteLength
        {
            get { return _OutputReportByteLength; }
            set { if (value < 2) throw new Exception("InputReportByteLength should be >1 ");  _OutputReportByteLength = value; }
        }



		internal Native.JoyInfoEx info;



		List<byte[]> _data;

		public List<byte[]> CompactDeviceData {
			get {
				if(_data==null) _data=new List<byte[]>(8);
				return _data;
			}
		}		
		
		public GenericHIDDevice(int index, int VID, int PID, IntPtr deviceHandle, IHIDInterface hidInterface, string devicePath, string name = ""):base(index,VID,PID,deviceHandle,hidInterface,devicePath,name)
        {
            try
            {
                //TODO find way to check if real device is connected cos it can be inside HID list but actually not connected

                var hidHandle = OpenDeviceIO(this.DevicePath,Native.ACCESS_NONE);
              
                CloseDeviceIO(hidHandle);

				info = new Native.JoyInfoEx();
				info.Size = Native.JoyInfoEx.SizeInBytes;
				info.Flags = Native.JoystickFlags.All;
			
				
				__lastHIDReport = new HIDReport(this.index, CreateInputBuffer(),HIDReport.ReadStatus.NoDataRead);
                
            }
            catch (Exception exception)
            {
                throw new Exception(string.Format("Error querying HID device '{0}'.", devicePath), exception);
            }
        }


      


      


        #region IHIDDeviceInfo implementation




        public void OpenDevice()
        {
        
            if (IsOpen) return;

      

            try
            {
                ReadHandle = OpenDeviceIO(this.DevicePath, DeviceMode.NonOverlapped, Native.GENERIC_READ);
                WriteHandle = OpenDeviceIO(this.DevicePath, DeviceMode.NonOverlapped, Native.GENERIC_WRITE);
                ReadAsyncHandle = OpenDeviceIO(this.DevicePath, DeviceMode.Overlapped, Native.GENERIC_READ);
                WriteAsyncHandle = OpenDeviceIO(this.DevicePath, DeviceMode.Overlapped, Native.GENERIC_WRITE);
            }
            catch (Exception exception)
            {
                IsOpen = false;
                throw new Exception("Error opening HID device.", exception);
            }

            IsOpen = ReadHandle.ToInt32() != Native.INVALID_HANDLE_VALUE & WriteHandle.ToInt32() != Native.INVALID_HANDLE_VALUE;
        }

       

        public void CloseDevice()
        {
            if (!IsOpen) return;
            CloseDeviceIO(ReadHandle);
            CloseDeviceIO(WriteHandle);

            CloseDeviceIO(ReadAsyncHandle);
            CloseDeviceIO(WriteAsyncHandle);

            UnityEngine.Debug.Log("Clossing device handles");

                ReadHandle=IntPtr.Zero;
                 WriteHandle=IntPtr.Zero;
                

            IsOpen = false;
        }


		public override HIDReport Read ()
		{
			//thru to get joystick info
			Native.JoystickError result = Native.joyGetPosEx(__lastHIDReport.index, ref info);

			
			if (result == Native.JoystickError.NoError) {


								CompactDeviceData [0] = BitConverter.GetBytes (info.Buttons);//4B
								CompactDeviceData [1] = BitConverter.GetBytes (info.Pov);//2B	
								CompactDeviceData [2] = BitConverter.GetBytes (info.XPos);//4B
								CompactDeviceData [3] = BitConverter.GetBytes (info.YPos);
								CompactDeviceData [4] = BitConverter.GetBytes (info.ZPos);
								CompactDeviceData [5] = BitConverter.GetBytes (info.RPos);
								CompactDeviceData [6] = BitConverter.GetBytes (info.UPos);
								CompactDeviceData [7] = BitConverter.GetBytes (info.VPos);

								byte[] compactByteArray = new byte[ 30 ];
								int inx = 0;
								int len;

								for (int i=0; i<8; i++) {
										len = CompactDeviceData [i].Length;
										System.Buffer.BlockCopy (CompactDeviceData [i], 0, compactByteArray, inx, len);
										inx += len;
					
								}
				

				
								__lastHIDReport.Data = compactByteArray;
								__lastHIDReport.Status = HIDReport.ReadStatus.Success;
				
						} else {
							__lastHIDReport.Data=new byte[1];
							__lastHIDReport.Status=HIDReport.ReadStatus.ReadError;
						}


			return __lastHIDReport;
		}
		
		
		override public void Read(ReadCallback callback)
		{
			Read(callback, 0);
		}
		
		override public void Read(ReadCallback callback,int timeout)
		{
			if (IsReadInProgress)
			{
				//UnityEngine.Debug.Log("Clone paket");
				__lastHIDReport.Status = HIDReport.ReadStatus.Resent;
				callback.BeginInvoke(__lastHIDReport, EndReadCallback, callback);
				// callback.Invoke(__lastHIDReport);
				return;
			}
			
			IsReadInProgress = true;
			
			//TODO make this fields or use pool
			var readDelegate = new ReadDelegate(Read);
			var asyncState = new HidAsyncState(readDelegate, callback);
			readDelegate.BeginInvoke(timeout,EndRead, asyncState);
		}
		
		
		
		
		
		protected HIDReport Read(int timeout)
		{
			
			if (IsOpen == false) OpenDevice();
			try
			{
				return ReadData(timeout);
                }
                catch
                {
                    return new HIDReport(-1,null,HIDReport.ReadStatus.ReadError);
                }

           
           
        }

        public override void Write(object data, HIDDevice.WriteCallback callback,int timeout)
        {

            var writeDelegate = new WriteDelegate(Write);
            var asyncState = new HidAsyncState(writeDelegate, callback);
            writeDelegate.BeginInvoke((byte[])data,timeout, EndWrite, asyncState);

        }

        public override void Write(object data, HIDDevice.WriteCallback callback)
        {
            this.Write((byte[])data,callback, 0);
        }

        /// <summary>
        /// Syncro write timeout
        /// </summary>
        /// <param name="data"></param>
        /// <param name="timeout"></param>
        public override void Write(object data,int timeout)
        {
            this.WriteData((byte[])data, timeout);
        }

        /// <summary>
        /// Syncro write
        /// </summary>
        /// <param name="data"></param>
        public override void Write(object data)
        {
            this.WriteData((byte[])data,0);
        }
       

     

        protected bool Write(byte[] data, int timeout)
        {
            
                if (IsOpen == false) OpenDevice();
                try
                {
                    return WriteData(data, timeout);
                }
                catch
                {
                    return false;
                }
            
        }

      

     

        override public void Dispose()
        {
           

            if (IsOpen) CloseDevice();
        }



        #endregion

        #region private

        protected void EndRead(IAsyncResult ar)
        {
           
            var hidAsyncState = (HidAsyncState)ar.AsyncState;
            var callerDelegate = (ReadDelegate)hidAsyncState.CallerDelegate;
            var callbackDelegate = (ReadCallback)hidAsyncState.CallbackDelegate;
            var data = callerDelegate.EndInvoke(ar);


            if ((callbackDelegate != null)) callbackDelegate.BeginInvoke(data, EndReadCallback, callbackDelegate);

            //if ((callbackDelegate != null)) callbackDelegate.Invoke(data);

            IsReadInProgress = false;
        }


        protected void EndReadCallback(IAsyncResult ar)
        {
            // Because you passed your original delegate in the asyncState parameter
            // of the Begin call, you can get it back here to complete the call.
            ReadCallback dlgt = (ReadCallback)ar.AsyncState;

            // Complete the call.
            dlgt.EndInvoke(ar);
        }

       
        private void EndWrite(IAsyncResult ar)
        {
            var hidAsyncState = (HidAsyncState)ar.AsyncState;
            var callerDelegate = (WriteDelegate)hidAsyncState.CallerDelegate;
            var callbackDelegate = (WriteCallback)hidAsyncState.CallbackDelegate;
            var result = callerDelegate.EndInvoke(ar);

            if ((callbackDelegate != null)) callbackDelegate.BeginInvoke(result, EndWriteCallback, callbackDelegate);
            //if ((callbackDelegate != null)) callbackDelegate.Invoke(result);
        }


        protected void EndWriteCallback(IAsyncResult ar)
        {
            // Because you passed your original delegate in the asyncState parameter
            // of the Begin call, you can get it back here to complete the call.
            WriteCallback dlgt = (WriteCallback)ar.AsyncState;
          
            // Complete the call.
            dlgt.EndInvoke(ar);
        }
            

       

        private byte[] CreateInputBuffer()
        {
            return CreateBuffer((int)InputReportByteLength - 1);
        }

        private byte[] CreateOutputBuffer()
        {
            return CreateBuffer((int)OutputReportByteLength - 1);
        }

        

        private static byte[] CreateBuffer(int length)
        {
            byte[] buffer = null;
            Array.Resize(ref buffer, length + 1);
            return buffer;
        }

       

      

        private bool WriteData(byte[] data, int timeout)
        {
            

            var buffer = CreateOutputBuffer();
            uint bytesWritten = 0;

            Array.Copy(data, 0, buffer, 0, Math.Min(data.Length, OutputReportByteLength));

            if (timeout>0)
            {
                
                var security = new Native.SECURITY_ATTRIBUTES();
               
                var overlapped = new NativeOverlapped();

               // var overlapTimeout = timeout <= 0 ? Native.WAIT_INFINITE : timeout;
                var overlapTimeout = timeout;

                security.lpSecurityDescriptor = IntPtr.Zero;
                security.bInheritHandle = true;
                security.nLength = Marshal.SizeOf(security);

                overlapped.OffsetLow = 0;
                overlapped.OffsetHigh = 0;
                overlapped.EventHandle = Native.CreateEvent(ref security, Convert.ToInt32(false), Convert.ToInt32(true), "");

                bool success;

                
                    success = Native.WriteFile(WriteAsyncHandle, buffer, (uint)buffer.Length, out bytesWritten, ref overlapped);
                    UnityEngine.Debug.Log("WriteFile happend " + success + " " + bytesWritten);

                    if (Marshal.GetLastWin32Error() > 0)
                    {
                        UnityEngine.Debug.LogWarning("Error during Write Data"+Marshal.GetLastWin32Error());
                    }
              
               // UnityEngine.Debug.LogError(Marshal.GetLastWin32Error());


                if (!success && Marshal.GetLastWin32Error() == Native.ERROR_IO_PENDING)
                {
                    var result = Native.WaitForSingleObject(overlapped.EventHandle, overlapTimeout);

                    //TODO clean overlapped
                    // System.Threading.Overlapped.Unpack(overlapped);

                    switch (result)
                    {
                        case Native.WAIT_OBJECT_0:
                          
                           
                            return true;
                        case Native.WAIT_TIMEOUT:
                            UnityEngine.Debug.Log("WriteData WAIT_TIMEOUT");
                            return false;
                        case Native.WAIT_FAILED:
                            UnityEngine.Debug.Log("WriteData WAIT_FAILED");
                            return false;
                        default:
                            return false;
                    }
                }

                return success;
            }
            else
            {
                try
                {
                    var overlapped = new NativeOverlapped();
                    bool success;
                    success = Native.WriteFile(WriteHandle, buffer, (uint)buffer.Length, out bytesWritten, ref overlapped);

                    if (!success)
                    {
                        UnityEngine.Debug.LogWarning(Marshal.GetLastWin32Error().ToString());
                    }

                    return success;

                }
                catch(Exception ex) {
                    UnityEngine.Debug.LogException(ex);
                    return false; }
            }
        }

        protected HIDReport ReadData(int timeout)
        {
            var buffer = new byte[] { };
            var status = HIDReport.ReadStatus.NoDataRead;
            int error = 0;
            var success = false;

            if (InputReportByteLength > 0)
            {
                uint bytesRead = 0;

                buffer = CreateInputBuffer();

                if (timeout>0)
                {
                    var security = new Native.SECURITY_ATTRIBUTES();
                    var overlapped = new NativeOverlapped();
                    var overlapTimeout =  timeout;

                    security.lpSecurityDescriptor = IntPtr.Zero;
                    security.bInheritHandle = true;
                    security.nLength = Marshal.SizeOf(security);

                    overlapped.OffsetLow = 0;
                    overlapped.OffsetHigh = 0;
                    overlapped.EventHandle = Native.CreateEvent(ref security, Convert.ToInt32(false), Convert.ToInt32(true), string.Empty);

                    try
                    {
                       success=Native.ReadFile(ReadAsyncHandle, buffer, (uint)buffer.Length, out bytesRead, ref overlapped);

                       UnityEngine.Debug.Log("Read happend " + success + " " + bytesRead);

                        error=Marshal.GetLastWin32Error();

                       if (error > 0)
                       {
                           UnityEngine.Debug.LogWarning("Error during Read Data" + error);
                       }



                       if (!success && (error == Native.ERROR_IO_PENDING || bytesRead < buffer.Length))
                       {
                           UnityEngine.Debug.LogWarning("Wait reading...");

                           var result = Native.WaitForSingleObject(overlapped.EventHandle, overlapTimeout);

                           switch (result)
                           {
                               case Native.WAIT_OBJECT_0: status = HIDReport.ReadStatus.Success; break;
                               case Native.WAIT_TIMEOUT:
                                   status = HIDReport.ReadStatus.WaitTimedOut;
                                   UnityEngine.Debug.Log("ReadData_WAIT_TIMEOUT");
                                   // buffer = new byte[] { };
                                   break;
                               case Native.WAIT_FAILED:
                                   status = HIDReport.ReadStatus.WaitFail;
                                   UnityEngine.Debug.Log("ReadData_WAIT_FAILED");
                                   //  buffer = new byte[] { };
                                   break;
                                
                               case Native.WAIT_ABANDONED:
                                   status = HIDReport.ReadStatus.Success;
                                    
                                    UnityEngine.Debug.Log("ReadData_WAIT_ABANDONED" + result);
                                //   buffer = new byte[] { };
                               break;

                               default:
                                    status = HIDReport.ReadStatus.NoDataRead;
                                
                                   UnityEngine.Debug.Log("ReadData Default" + result);
                                  
                                   break;
                           }
                       }
                       else
                       {
                           status = HIDReport.ReadStatus.Success;
                       }

                       
                    }
                    catch { status = HIDReport.ReadStatus.ReadError; }
                    finally { CloseDeviceIO(overlapped.EventHandle); }
                }
                else
                {
                    try
                    {
                        var overlapped = new NativeOverlapped();

                        success=Native.ReadFile(ReadHandle, buffer, (uint)buffer.Length, out bytesRead, ref overlapped);

                        error = Marshal.GetLastWin32Error();

                        status = HIDReport.ReadStatus.Success;

                        if (error > 0)
                        {
                            status = HIDReport.ReadStatus.ReadError;
                            UnityEngine.Debug.LogWarning("Error during Read Data" + error);
                        }

                       
                    }
                    catch { status = HIDReport.ReadStatus.ReadError; }
                }
            }


            __lastHIDReport.Data = buffer;

            __lastHIDReport.index = this.index;

            __lastHIDReport.Status = status;


            return __lastHIDReport;// new HIDReport(this.index, buffer, status);
        }

        private static IntPtr OpenDeviceIO(string devicePath, uint deviceAccess)
        {
           // return OpenDeviceIO(devicePath, DeviceMode.Overlapped, deviceAccess);
            return OpenDeviceIO(devicePath, DeviceMode.NonOverlapped, deviceAccess);
        }

        private static IntPtr OpenDeviceIO(string devicePath, DeviceMode deviceMode, uint deviceAccess)
        {
            var security = new Native.SECURITY_ATTRIBUTES();
            var flags = 0;

            if (deviceMode == DeviceMode.Overlapped) flags = Native.FILE_FLAG_OVERLAPPED;

            security.lpSecurityDescriptor = IntPtr.Zero;
            security.bInheritHandle = true;
            security.nLength = Marshal.SizeOf(security);


            //Handle = CreateFile(didetail->DevicePath, GENERIC_READ|GENERIC_WRITE,
												//	FILE_SHARE_READ,
												//	NULL, OPEN_EXISTING,
												//	FILE_FLAG_OVERLAPPED, NULL);

            return Native.CreateFile(devicePath, deviceAccess, Native.FILE_SHARE_READ | Native.FILE_SHARE_WRITE, ref security, Native.OPEN_EXISTING, flags, 0);
        }

        private static void CloseDeviceIO(IntPtr handle)
        {
            if (Environment.OSVersion.Version.Major > 5)
            {
                Native.CancelIoEx(handle, IntPtr.Zero);
            }
            Native.CloseHandle(handle);
        }

        #endregion
    }
}
#endif