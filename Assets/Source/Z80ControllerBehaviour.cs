using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Z80ControllerBehaviour : MonoBehaviour {

    private Z80CPU cpu = new Z80CPU();

    // Use this for initialization
    void Start () {
        UnitySystemConsoleRedirector.Redirect();

        Console.WriteLine("CPU Speed: " + cpu.CPUSpeed);
        Console.WriteLine("Cycle time: " + cpu.CycleTime);

        // load a rom into memory
        //string romPath = "/cpu_instrs.gb";
        string romPath = "/individual/01-special.gb";
        //string romPath = "/individual/06-ld r,r.gb";
        //string romPath = "/individual/07-jr,jp,call,ret,rst.gb";
        //string romPath = "/individual/08-misc instrs.gb";
        byte[] rom = System.IO.File.ReadAllBytes(Application.streamingAssetsPath + romPath);
        byte[] name = new byte[16];
        System.Array.Copy(rom, 0x0134, name, 0, 16);
        System.Console.WriteLine("Title: " + System.Text.Encoding.ASCII.GetString(name));
        System.Console.WriteLine("Size: " + rom.Length);

        if (rom == null) {
            Debug.Log("REFUSED TO LOAD RESOURCE! D8");
            return;
        }
        cpu.SetMemory(0, rom);
        cpu.StartSerialThread();
    }

    float _serialPrintCounter = 0;

    // Update is called once per frame
    void Update () {
        //cpu.RunForTimeChunk(Time.deltaTime);
		//cpu.RunForTimeChunk(0);
        cpu.RunForTimeChunk(1 / 60.0f);

        _serialPrintCounter += Time.deltaTime;
		if (_serialPrintCounter > 2) {
            Debug.Log(cpu.GetSerialString());
            _serialPrintCounter = 0;
        }
    }
}
