#pragma once

#using <WindowsBase.dll>

#include <frida-core.h>
#include <msclr/gcroot.h>

using namespace System;

namespace Frida
{
  ref class Script;
  enum class SessionDetachReason;
  ref class SessionDetachedEventArgs;
  public delegate void SessionDetachedHandler (Object ^ sender, SessionDetachedEventArgs ^ e);

  public ref class Session
  {
  internal:
    Session (FridaSession * handle);
  public:
    ~Session ();
  protected:
    !Session ();

  public:
    event SessionDetachedHandler ^ Detached;

    property unsigned int Pid { unsigned int get (); }

    void Detach ();
    Script ^ CreateScript (String ^ source);
    Script ^ CreateScript (String ^ source, String ^ name);

  internal:
    void OnDetached (Object ^ sender, SessionDetachedEventArgs ^ e);

  private:
    FridaSession * handle;
    msclr::gcroot<Session ^> * selfHandle;

    SessionDetachedHandler ^ onDetachedHandler;
  };

  public enum class SessionDetachReason
  {
    ApplicationRequested = 1,
    ProcessReplaced,
    ProcessTerminated,
    ConnectionTerminated,
    DeviceLost
  };

  public ref class SessionDetachedEventArgs : public EventArgs
  {
  public:
    property SessionDetachReason Reason { SessionDetachReason get () { return reason; } };

    SessionDetachedEventArgs (SessionDetachReason reason)
    {
      this->reason = reason;
    }

  private:
    SessionDetachReason reason;
  };
}
