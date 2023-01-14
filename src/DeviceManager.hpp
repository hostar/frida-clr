#pragma once

#using <WindowsBase.dll>

#include <frida-core.h>
#include <msclr/gcroot.h>

using namespace System;

namespace Frida
{
  ref class Device;

  public ref class DeviceManager
  {
  public:
    DeviceManager ();
    ~DeviceManager ();
  protected:
    !DeviceManager ();

  public:
    event EventHandler ^ Changed;

    array<Device ^> ^ EnumerateDevices ();

  internal:
    void OnChanged (Object ^ sender, EventArgs ^ e);

  private:
    FridaDeviceManager * handle;
    msclr::gcroot<DeviceManager ^> * selfHandle;

    EventHandler ^ onChangedHandler;
  };
}