namespace QtCore {

	using System;
	using System.Reflection;
	using System.Collections;
	using System.Runtime.InteropServices;
	using System.Text;

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void EventFunc();

	class EventReceiver : QObject {
		public EventReceiver(QObject parent) : base(parent) {}
		
		public override bool OnEvent(QEvent e) {
			if (e != null && e.type() == QEvent.Type.User) {
				ThreadEvent my = e as ThreadEvent;
				if (e != null) {
					my.dele();
					my.handle.Free();  // free the handle so the event can be collected
					return true;
				}
			}
			return false;
		}
	}

	class ThreadEvent : QEvent {
		public EventFunc dele;
		public GCHandle handle;
		
		public ThreadEvent(EventFunc d) : base(QEvent.Type.User) {
			dele = d;
			GC.SuppressFinalize(this);    // once the event has entered the event loop, Qt will care for it
		}
	}

	public partial class QCoreApplication : QObject, IDisposable {
		private EventReceiver receiver;
		protected void SetupEventReceiver() { receiver = new EventReceiver(this); }

		public static void Invoke(EventFunc dele) {
			ThreadEvent e = new ThreadEvent(dele);
			e.handle = GCHandle.Alloc(e);  // we don't want the GC to collect the event too early
			PostEvent(qApp.receiver, e);
		}

		string[] GenerateArgs(string[] argv)
		{
			string[] args = new string[argv.Length + 1];
			Assembly a = System.Reflection.Assembly.GetEntryAssembly();

			if(a == null)
				a = System.Reflection.Assembly.GetExecutingAssembly();

			object[] attrs = a.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
			if (attrs.Length > 0) {
				args[0] = ((AssemblyTitleAttribute) attrs[0]).Title;
			} else {
				QFileInfo info = new QFileInfo(a.Location);
				args[0] = info.BaseName();
			}
			argv.CopyTo(args, 1);

			return args;
		}

		public QCoreApplication(string[] argv) : this((Type) null) {
			CreateProxy();
			
			string[] args = GenerateArgs(argv);
			
			interceptor.Invoke(	"QCoreApplication$?", 
								"QCoreApplication(int&, char**)", 
								typeof(void), false, typeof(int), args.Length, typeof(string[]), args );
			SetupEventReceiver();
		}

		public static int Exec() {
			int result = (int) staticInterceptor.Invoke("exec", "exec()", typeof(int), false);
			Qyoto.Cleanup();
			return result;
		}
	}
}
