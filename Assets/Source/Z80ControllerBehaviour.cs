using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Z80ControllerBehaviour : MonoBehaviour {

    private Z80CPU cpu = new Z80CPU();

    // Use this for initialization
    void Start () {
        // load a rom into memory
        byte[] rom = System.IO.File.ReadAllBytes(Application.streamingAssetsPath + "/cpu_instrs.gb");
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

        _serialPrintCounter += Time.deltaTime;
		if (_serialPrintCounter > 2) {
            Debug.Log(cpu.GetSerialString());
            _serialPrintCounter = 0;
        }
    }
}
