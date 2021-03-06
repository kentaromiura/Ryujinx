using ChocolArm64.Memory;
using Ryujinx.OsHle.Handles;
using Ryujinx.OsHle.Ipc;
using System.Collections.Generic;
using System.IO;

using static Ryujinx.OsHle.Objects.Android.Parcel;
using static Ryujinx.OsHle.Objects.ObjHelper;

namespace Ryujinx.OsHle.Objects.Vi
{
    class IApplicationDisplayService : IIpcInterface
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        public IApplicationDisplayService()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                {  100, GetRelayService                      },
                {  101, GetSystemDisplayService              },
                {  102, GetManagerDisplayService             },
                {  103, GetIndirectDisplayTransactionService },
                { 1010, OpenDisplay                          },
                { 2020, OpenLayer                            },
                { 2030, CreateStrayLayer                     },
                { 2101, SetLayerScalingMode                  },
                { 5202, GetDisplayVSyncEvent                 }
            };
        }

        public long GetRelayService(ServiceCtx Context)
        {
            MakeObject(Context, new IHOSBinderDriver());

            return 0;
        }

        public long GetSystemDisplayService(ServiceCtx Context)
        {
            MakeObject(Context, new ISystemDisplayService());

            return 0;
        }

        public long GetManagerDisplayService(ServiceCtx Context)
        {
            MakeObject(Context, new IManagerDisplayService());

            return 0;
        }

        public long GetIndirectDisplayTransactionService(ServiceCtx Context)
        {
            MakeObject(Context, new IHOSBinderDriver());

            return 0;
        }

        public long OpenDisplay(ServiceCtx Context)
        {
            string Name = GetDisplayName(Context);

            long DisplayId = Context.Ns.Os.Displays.GenerateId(new Display(Name));

            Context.ResponseData.Write(DisplayId);

            return 0;
        }

        public long OpenLayer(ServiceCtx Context)
        {
            long LayerId = Context.RequestData.ReadInt64();
            long UserId  = Context.RequestData.ReadInt64();

            long ParcelPtr = Context.Request.ReceiveBuff[0].Position;

            byte[] Parcel = MakeIGraphicsBufferProducer(ParcelPtr);

            AMemoryHelper.WriteBytes(Context.Memory, ParcelPtr, Parcel);

            Context.ResponseData.Write((long)Parcel.Length);

            return 0;
        }

        public long CreateStrayLayer(ServiceCtx Context)
        {
            long LayerFlags = Context.RequestData.ReadInt64();
            long DisplayId  = Context.RequestData.ReadInt64();

            long ParcelPtr = Context.Request.ReceiveBuff[0].Position;

            Display Disp = Context.Ns.Os.Displays.GetData<Display>((int)DisplayId);

            byte[] Parcel = MakeIGraphicsBufferProducer(ParcelPtr);

            AMemoryHelper.WriteBytes(Context.Memory, ParcelPtr, Parcel);

            Context.ResponseData.Write(0L);
            Context.ResponseData.Write((long)Parcel.Length);

            return 0;
        }

        public long SetLayerScalingMode(ServiceCtx Context)
        {
            int  ScalingMode = Context.RequestData.ReadInt32();
            long Unknown     = Context.RequestData.ReadInt64();

            return 0;
        }

        public long GetDisplayVSyncEvent(ServiceCtx Context)
        {
            string Name = GetDisplayName(Context);

            int Handle = Context.Ns.Os.Handles.GenerateId(new HEvent());

            Context.Response.HandleDesc = IpcHandleDesc.MakeCopy(Handle);

            return 0;
        }

        private byte[] MakeIGraphicsBufferProducer(long BasePtr)
        {
            long Id        = 0x20;
            long CookiePtr = 0L;

            using (MemoryStream MS = new MemoryStream())
            {
                BinaryWriter Writer = new BinaryWriter(MS);

                //flat_binder_object (size is 0x28)
                Writer.Write(2); //Type (BINDER_TYPE_WEAK_BINDER)
                Writer.Write(0); //Flags
                Writer.Write((int)(Id >> 0));
                Writer.Write((int)(Id >> 32));
                Writer.Write((int)(CookiePtr >> 0));
                Writer.Write((int)(CookiePtr >> 32));
                Writer.Write((byte)'d');
                Writer.Write((byte)'i');
                Writer.Write((byte)'s');
                Writer.Write((byte)'p');
                Writer.Write((byte)'d');
                Writer.Write((byte)'r');
                Writer.Write((byte)'v');
                Writer.Write((byte)'\0');
                Writer.Write(0L); //Pad

                return MakeParcel(MS.ToArray(), new byte[] { 0, 0, 0, 0 });
            }
        }

        private string GetDisplayName(ServiceCtx Context)
        {
            string Name = string.Empty;

            for (int Index = 0; Index < 8 &&
                Context.RequestData.BaseStream.Position <
                Context.RequestData.BaseStream.Length; Index++)
            {
                byte Chr = Context.RequestData.ReadByte();

                if (Chr >= 0x20 && Chr < 0x7f)
                {
                    Name += (char)Chr;
                }
            }

            return Name;
        }
    }
}