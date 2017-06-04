using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class Z80CPU {

    public double CPUSpeed
    {
        get { return _cpuSpeed; }
        set {
            _cpuSpeed = value;
            _cycleTime = 1.0 / _cpuSpeed;
        }
    }

    public double CycleTime
    {
        get { return _cycleTime; }
        set {
            _cycleTime = value;
            _cpuSpeed = 1.0 / _cycleTime;
        }
    }

    public Z80CPU()
    {
        // D = 0x00;

        // A = 0x01;
        // F = 0x00;
        // B = 0xFF;
        // E = 0xC1;
        // H = 0x84;
        // C = 0x13;
        // L = 0x03;
    }

    // Run the processor upt a maximum of the simulated time indicated in seconds.
    // It doesn't actually take that long (I hope!), but rather uses the clock
    // speed against a cycle counter and runs however many operations it would
    // normally get done until the indicated time slice has completed.
    //
    // This function may return early for any number of reasons, including the
    // the HALT and STOP commands getting called. However barring such scenarios
    // it guarantees at least one run through the emulator (even if seconds == 0).
    //
    // NOTE: It's likely that this naive implementation may not be compatible
    // with proper inputs. We'll have to see... :\
    public void RunForTimeChunk(float seconds)
    {
        _cycles = 0;
        int steps = 0;

        do
        { // at least once
            if (HALT || STOP)
                break;  // unless we are HALTed or STOPped.

            ++steps;
            Process(seconds == 0);
        } while (_cycles * _cycleTime * 4 < seconds && steps < 200000);

        if (steps == 200000) {
            System.Console.WriteLine("Hit 200,000 steps @ " + _cycles + " cycles");
        }
    }

    public bool Halted
    {
        get { return HALT; }
    }

    public bool Stopped
    {
        get { return STOP; }
    }

    public void SetMemory(ushort address, byte value)
    {
        if (address >= _ram.Length)
            throw new System.Exception("Tried to set ram past the end: @" + address + "; max: " + (_ram.Length - 1));

        _ram[address] = value;
    }

    public void SetMemory(ushort start, byte[] chunk)
    {
        if (start >= _ram.Length)
            throw new System.Exception("Tried to set ram past the end: @" + start + "; max: " + (_ram.Length - 1));
        
        if (start + chunk.Length > _ram.Length)
            throw new System.Exception("Set ram would overflow the end: @" + (start + chunk.Length) + "; max: " + (_ram.Length - 1));

        chunk.CopyTo(_ram, start);
    }

    //=========================================================================
    // Private variables required for the CPU to function
    private double _cpuSpeed = 4194304.0;
    private double _cycleTime = 1.0 / (4194304.0);

	private byte[] _ram = new byte[65536];
    private ushort SP = 0xfffe;  // stack pointer
    private ushort PC = 0x0100;  // program counter (the next instruction to execute)

    private ulong _cycles = 0;  // count of how many logic cycles have been processed since last count reset

	// These are often accessed in pairs to address memory.
	// I can't decide if it's better to use them as combined
	// shorts, individual bytes, or have both and sync them. :\
    private volatile byte A = 0x00;
	private volatile byte F = 0x00;
    private volatile byte B = 0x00;
	private volatile byte C = 0x00;
    private volatile byte D = 0x00;
	private volatile byte E = 0x00;
    private volatile byte H = 0x00;
	private volatile byte L = 0x00;

    // Flag (F) register masks
    private byte ZERO_FLAG = 1 << 7;
    private byte SUBT_FLAG = 1 << 6;
    private byte HALF_CARRY_FLAG = 1 << 5;
    private byte CARRY_FLAG = 1 << 4;

    private bool IME = true;  // Interrupt Master Enabled flag
    private byte VBLANK_IF = 1;
    private byte LCDC_IF = 1 << 1;
    private byte TIMER_OVERFLOW_IF = 1 << 2;
    private byte SERIAL_IO_COMPLETE_IF = 1 << 3;
    private byte P10_P13_TERM_NEG_EDGE_IF = 1 << 4;

    private bool HALT = false;
    private bool STOP = false;

    private const ushort IF_Addr = 0xFF0F;
    private const ushort IE_Addr = 0xFFFF;

    public const ushort SB_addr = 0xFF01;  // serial out
    public const ushort SC_addr = 0xFF02;  // serial control

    private bool _runSerialAccumulator = false;
    private List<byte> _serialOut = new List<byte>();

    public string GetSerialOut() {
        return System.Text.Encoding.ASCII.GetString(_serialOut.ToArray());
    }

    public void ClearSerialOut() {
        _serialOut.Clear();
    }

    void WriteRam(ushort addr, byte val) {
        _ram[addr] = val;

        // if (val == 0x88)
        //     Console.WriteLine("Writing 0x88");
        
        // if (addr == 0xff83)
        //     Console.WriteLine("Writing " + val + " to 0xFF83");

        if (addr == SC_addr && val == 0x81) {
            //Console.WriteLine("Wrote to serial! :: " + System.Text.Encoding.ASCII.GetString(new byte[]{val}));
            _serialOut.Add(_ram[SB_addr]);
            _ram[SC_addr] &= 0x0f;
        }
    }

    private enum OpCodes : byte
    {
        LD_A_A = 0x7F,
        LD_A_B = 0x78,
        LD_A_C = 0x79,
        LD_A_D = 0x7A,
        LD_A_E = 0x7B,
        LD_A_H = 0x7C,
        LD_A_L = 0x7D,
        LD_B_A = 0x47,
        LD_B_B = 0x40,
        LD_B_C = 0x41,
        LD_B_D = 0x42,
        LD_B_E = 0x43,
        LD_B_H = 0x44,
        LD_B_L = 0x45,
        LD_C_A = 0x4F,
        LD_C_B = 0x48,
        LD_C_C = 0x49,
        LD_C_D = 0x4A,
        LD_C_E = 0x4B,
        LD_C_H = 0x4C,
        LD_C_L = 0x4D,
        LD_D_A = 0x57,
        LD_D_B = 0x50,
        LD_D_C = 0x51,
        LD_D_D = 0x52,
        LD_D_E = 0x53,
        LD_D_H = 0x54,
        LD_D_L = 0x55,
        LD_E_A = 0x5F,
        LD_E_B = 0x58,
        LD_E_C = 0x59,
        LD_E_D = 0x5A,
        LD_E_E = 0x5B,
        LD_E_H = 0x5C,
        LD_E_L = 0x5D,
        LD_H_A = 0x67,
        LD_H_B = 0x60,
        LD_H_C = 0x61,
        LD_H_D = 0x62,
        LD_H_E = 0x63,
        LD_H_H = 0x64,
        LD_H_L = 0x65,
        LD_L_A = 0x6F,
        LD_L_B = 0x68,
        LD_L_C = 0x69,
        LD_L_D = 0x6A,
        LD_L_E = 0x6B,
        LD_L_H = 0x6C,
        LD_L_L = 0x6D,
        LD_A_N = 0x3E,
        LD_B_N = 0x06,
        LD_C_N = 0x0E,
        LD_D_N = 0x16,
        LD_E_N = 0x1E,
        LD_H_N = 0x26,
        LD_L_N = 0x2E,
        LD_A_mHL = 0x7E,
        LD_B_mHL = 0x46,
        LD_C_mHL = 0x4E,
        LD_D_mHL = 0x56,
        LD_E_mHL = 0x5E,
        LD_H_mHL = 0x66,
        LD_L_mHL = 0x6E,
        LD_mHL_A = 0x77,
        LD_mHL_B = 0x70,
        LD_mHL_C = 0x71,
        LD_mHL_D = 0x72,
        LD_mHL_E = 0x73,
        LD_mHL_H = 0x74,
        LD_mHL_L = 0x75,
        LD_mHL_N = 0x36,
        LD_A_mBC = 0x0A,
        LD_A_mDE = 0x1A,
        LD_A_mC = 0xF2,
        LD_mC_A = 0xE2,
        LD_A_mN = 0xF0,
        LD_mN_A = 0xE0,
        LD_A_mNN = 0xFA,
        LD_mNN_A = 0xEA,
        LD_A_HLI = 0x2A,
        LD_A_HLD = 0x3A,
        LD_mBC_A = 0x02,
        LD_mDE_A = 0x12,
        LD_HLI_A = 0x22,
        LD_HLD_A = 0x32,
        LD_BC_NN = 0x01,
        LD_DE_NN = 0x11,
        LD_HL_NN = 0x21,
        LD_SP_NN = 0x31,
        LD_SP_HL = 0xF9,
        PUSH_BC = 0xC5,
        PUSH_DE = 0xD5,
        PUSH_HL = 0xE5,
        PUSH_AF = 0xF5,
        POP_BC = 0xC1,
        POP_DE = 0xD1,
        POP_HL = 0xE1,
        POP_AF = 0xF1,
        LDHL_SP_e = 0xF8,
        LD_mNN_SP = 0x08,
        ADD_A_A = 0x87,
        ADD_A_B = 0x80,
        ADD_A_C = 0x81,
        ADD_A_D = 0x82,
        ADD_A_E = 0x83,
        ADD_A_H = 0x84,
        ADD_A_L = 0x85,
        ADD_A_N = 0xC6,
        ADD_A_mHL = 0x86,
        ADC_A_A = 0x8F,
        ADC_A_B = 0x88,
        ADC_A_C = 0x89,
        ADC_A_D = 0x8A,
        ADC_A_E = 0x8B,
        ADC_A_H = 0x8C,
        ADC_A_L = 0x8D,
        ADC_A_N = 0xCE,
        ADC_A_mHL = 0x8E,
        SUB_A = 0x97,
        SUB_B = 0x90,
        SUB_C = 0x91,
        SUB_D = 0x92,
        SUB_E = 0x93,
        SUB_H = 0x94,
        SUB_L = 0x95,
        SUB_N = 0xD6,
        SUB_mHL = 0x96,
        SBC_A_A = 0x9F,
        SBC_A_B = 0x98,
        SBC_A_C = 0x99,
        SBC_A_D = 0x9A,
        SBC_A_E = 0x9B,
        SBC_A_H = 0x9C,
        SBC_A_L = 0x9D,
        SBC_A_N = 0xDE,
        SBC_A_mHL = 0x9E,
        AND_A = 0xA7,
        AND_B = 0xA0,
        AND_C = 0xA1,
        AND_D = 0xA2,
        AND_E = 0xA3,
        AND_H = 0xA4,
        AND_L = 0xA5,
        AND_N = 0xE6,
        AND_mHL = 0xA6,
        OR_A = 0xB7,
        OR_B = 0xB0,
        OR_C = 0xB1,
        OR_D = 0xB2,
        OR_E = 0xB3,
        OR_H = 0xB4,
        OR_L = 0xB5,
        OR_N = 0xF6,
        OR_mHL = 0xB6,
        XOR_A = 0xAF,
        XOR_B = 0xA8,
        XOR_C = 0xA9,
        XOR_D = 0xAA,
        XOR_E = 0xAB,
        XOR_H = 0xAC,
        XOR_L = 0xAD,
        XOR_N = 0xEE,
        XOR_mHL = 0xAE,
        CP_A = 0xBF,
        CP_B = 0xB8,
        CP_C = 0xB9,
        CP_D = 0xBA,
        CP_E = 0xBB,
        CP_H = 0xBC,
        CP_L = 0xBD,
        CP_N = 0xFE,
        CP_mHL = 0xBE,
        INC_A = 0x3C,
        INC_B = 0x04,
        INC_C = 0x0C,
        INC_D = 0x14,
        INC_E = 0x1C,
        INC_H = 0x24,
        INC_L = 0x2C,
        INC_mHL = 0x34,
        DEC_A = 0x3D,
        DEC_B = 0x05,
        DEC_C = 0x0D,
        DEC_D = 0x15,
        DEC_E = 0x1D,
        DEC_H = 0x25,
        DEC_L = 0x2D,
        DEC_mHL = 0x35,
        ADD_HL_BC = 0x09,
        ADD_HL_DE = 0x19,
        ADD_HL_HL = 0x29,
        ADD_HL_SP = 0x39,
        ADD_SP_e = 0xE8,
        INC_BC = 0x03,
        INC_DE = 0x13,
        INC_HL = 0x23,
        INC_SP = 0x33,
        DEC_BC = 0x0B,
        DEC_DE = 0x1B,
        DEC_HL = 0x2B,
        DEC_SP = 0x3B,
        RLCA = 0x07,
        RLA = 0x17,
        RRCA = 0x0F,
        RRA = 0x1F,
        MULTI_BYTE_OP = 0xCB,
        JP_NN = 0xC3,
        JP_NZ_NN = 0xC2,
        JP_Z_NN = 0xCA,
        JP_NC_NN = 0xD2,
        JP_C_NN = 0xDA,
        JR_e = 0x18,
        JR_NZ_e = 0x20,
        JR_Z_e = 0x28,
        JR_NC_e = 0x30,
        JR_C_e = 0x38,
        JP_mHL = 0xE9,
        CALL_NN = 0xCD,
        CALL_NZ_NN = 0xC4,
        CALL_Z_NN = 0xCC,
        CALL_NC_NN = 0xD4,
        CALL_C_NN = 0xDC,
        RET = 0xC9,
        RETI = 0xD9,
        RET_NZ = 0xC0,
        RET_Z = 0xC8,
        RET_NC = 0xD0,
        RET_C = 0xD8,
        RST_0 = 0xC7,
        RST_1 = 0xCF,
        RST_2 = 0xD7,
        RST_3 = 0xDF,
        RST_4 = 0xE7,
        RST_5 = 0xEF,
        RST_6 = 0xF7,
        RST_7 = 0xFF,
        DAA = 0x27,
        CPL = 0x2F,
        NOP = 0x00,
        HALT = 0x76,
        STOP = 0x10,
        EI = 0xF3,
        DI = 0xFB
    }

    enum MultyByteOpcode
    {
        RLC_m = 0x00,
        RL_m = 0x02,
        RRC_m = 0x01,
        RR_m = 0x03,
        SLA_m = 0x04,
        SRA_m = 0x05,
        SRL_m = 0x07,
        SWAP_m = 0x06,
        SET_b_r = 0xCB,
        RES_b_r = 0xCB,
    }

    private struct OpMetaData
    {
        public uint cycleCount;
        public byte counterShift;
    }

    private Dictionary<OpCodes, OpMetaData> OpInfo = new Dictionary<OpCodes, OpMetaData>()
    {
        { OpCodes.LD_A_A, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_A_B, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_A_C, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_A_D, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_A_E, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_A_H, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_A_L, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_B_A, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_B_B, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_B_C, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_B_D, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_B_E, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_B_H, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_B_L, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_C_A, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_C_B, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_C_C, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_C_D, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_C_E, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_C_H, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_C_L, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_D_A, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_D_B, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_D_C, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_D_D, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_D_E, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_D_H, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_D_L, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_E_A, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_E_B, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_E_C, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_E_D, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_E_E, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_E_H, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_E_L, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_H_A, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_H_B, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_H_C, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_H_D, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_H_E, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_H_H, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_H_L, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_L_A, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_L_B, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_L_C, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_L_D, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_L_E, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_L_H, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_L_L, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.LD_A_N, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.LD_B_N, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.LD_C_N, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.LD_D_N, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.LD_E_N, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.LD_H_N, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.LD_L_N, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.LD_A_mHL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_B_mHL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_C_mHL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_D_mHL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_E_mHL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_H_mHL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_L_mHL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_mHL_A, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_mHL_B, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_mHL_C, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_mHL_D, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_mHL_E, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_mHL_H, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_mHL_L, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_mHL_N, new OpMetaData() { cycleCount = 3, counterShift = 2 } },
        { OpCodes.LD_A_mBC, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_A_mDE, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_A_mC, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_mC_A, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_A_mN, new OpMetaData() { cycleCount = 3, counterShift = 2 } },
        { OpCodes.LD_mN_A, new OpMetaData() { cycleCount = 3, counterShift = 2 } },
        { OpCodes.LD_A_mNN, new OpMetaData() { cycleCount = 4, counterShift = 3 } },
        { OpCodes.LD_mNN_A, new OpMetaData() { cycleCount = 4, counterShift = 3 } },
        { OpCodes.LD_A_HLI, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_A_HLD, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_mBC_A, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_mDE_A, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_HLI_A, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_HLD_A, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.LD_BC_NN, new OpMetaData() { cycleCount = 3, counterShift = 3 } },
        { OpCodes.LD_DE_NN, new OpMetaData() { cycleCount = 3, counterShift = 3 } },
        { OpCodes.LD_HL_NN, new OpMetaData() { cycleCount = 3, counterShift = 3 } },
        { OpCodes.LD_SP_NN, new OpMetaData() { cycleCount = 3, counterShift = 3 } },
        { OpCodes.LD_SP_HL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.PUSH_BC, new OpMetaData() { cycleCount = 4, counterShift = 1 } },
        { OpCodes.PUSH_DE, new OpMetaData() { cycleCount = 4, counterShift = 1 } },
        { OpCodes.PUSH_HL, new OpMetaData() { cycleCount = 4, counterShift = 1 } },
        { OpCodes.PUSH_AF, new OpMetaData() { cycleCount = 4, counterShift = 1 } },
        { OpCodes.POP_BC, new OpMetaData() { cycleCount = 3, counterShift = 1 } },
        { OpCodes.POP_DE, new OpMetaData() { cycleCount = 3, counterShift = 1 } },
        { OpCodes.POP_HL, new OpMetaData() { cycleCount = 3, counterShift = 1 } },
        { OpCodes.POP_AF, new OpMetaData() { cycleCount = 3, counterShift = 1 } },
        { OpCodes.LDHL_SP_e, new OpMetaData() { cycleCount = 3, counterShift = 2 } },
        { OpCodes.LD_mNN_SP, new OpMetaData() { cycleCount = 5, counterShift = 3 } },
        { OpCodes.ADD_A_A, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.ADD_A_B, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.ADD_A_C, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.ADD_A_D, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.ADD_A_E, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.ADD_A_H, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.ADD_A_L, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.ADD_A_N, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.ADD_A_mHL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.ADC_A_A, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.ADC_A_B, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.ADC_A_C, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.ADC_A_D, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.ADC_A_E, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.ADC_A_H, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.ADC_A_L, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.ADC_A_N, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.ADC_A_mHL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.SUB_A, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.SUB_B, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.SUB_C, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.SUB_D, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.SUB_E, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.SUB_H, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.SUB_L, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.SUB_N, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.SUB_mHL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.SBC_A_A, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.SBC_A_B, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.SBC_A_C, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.SBC_A_D, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.SBC_A_E, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.SBC_A_H, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.SBC_A_L, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.SBC_A_N, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.SBC_A_mHL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.AND_A, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.AND_B, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.AND_C, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.AND_D, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.AND_E, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.AND_H, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.AND_L, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.AND_N, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.AND_mHL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.OR_A, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.OR_B, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.OR_C, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.OR_D, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.OR_E, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.OR_H, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.OR_L, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.OR_N, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.OR_mHL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.XOR_A, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.XOR_B, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.XOR_C, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.XOR_D, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.XOR_E, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.XOR_H, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.XOR_L, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.XOR_N, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.XOR_mHL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.CP_A, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.CP_B, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.CP_C, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.CP_D, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.CP_E, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.CP_H, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.CP_L, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.CP_N, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.CP_mHL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.INC_A, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.INC_B, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.INC_C, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.INC_D, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.INC_E, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.INC_H, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.INC_L, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.INC_mHL, new OpMetaData() { cycleCount = 3, counterShift = 1 } },
        { OpCodes.DEC_A, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.DEC_B, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.DEC_C, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.DEC_D, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.DEC_E, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.DEC_H, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.DEC_L, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.DEC_mHL, new OpMetaData() { cycleCount = 3, counterShift = 1 } },
        { OpCodes.ADD_HL_BC, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.ADD_HL_DE, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.ADD_HL_HL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.ADD_HL_SP, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.ADD_SP_e, new OpMetaData() { cycleCount = 4, counterShift = 2 } },
        { OpCodes.INC_BC, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.INC_DE, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.INC_HL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.INC_SP, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.DEC_BC, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.DEC_DE, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.DEC_HL, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.DEC_SP, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.RLCA, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.RLA, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.RRCA, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.RRA, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.MULTI_BYTE_OP, new OpMetaData() { cycleCount = 0, counterShift = 0 } }, // These are handled elsewhere
        { OpCodes.JP_NN, new OpMetaData() { cycleCount = 4, counterShift = 0 } },
        { OpCodes.JP_NZ_NN, new OpMetaData() { cycleCount = 3, counterShift = 3 } },
        { OpCodes.JP_Z_NN, new OpMetaData() { cycleCount = 3, counterShift = 3 } },
        { OpCodes.JP_NC_NN, new OpMetaData() { cycleCount = 3, counterShift = 3 } },
        { OpCodes.JP_C_NN, new OpMetaData() { cycleCount = 3, counterShift = 3 } },
        { OpCodes.JR_e, new OpMetaData() { cycleCount = 3, counterShift = 2 } },
        { OpCodes.JR_NZ_e, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.JR_Z_e, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.JR_NC_e, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.JR_C_e, new OpMetaData() { cycleCount = 2, counterShift = 2 } },
        { OpCodes.JP_mHL, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.CALL_NN, new OpMetaData() { cycleCount = 6, counterShift = 3 } },
        { OpCodes.CALL_NZ_NN, new OpMetaData() { cycleCount = 3, counterShift = 3 } },
        { OpCodes.CALL_Z_NN, new OpMetaData() { cycleCount = 3, counterShift = 3 } },
        { OpCodes.CALL_NC_NN, new OpMetaData() { cycleCount = 3, counterShift = 3 } },
        { OpCodes.CALL_C_NN, new OpMetaData() { cycleCount = 3, counterShift = 3 } },
        { OpCodes.RET, new OpMetaData() { cycleCount = 4, counterShift = 1 } },
        { OpCodes.RETI, new OpMetaData() { cycleCount = 4, counterShift = 1 } },
        { OpCodes.RET_NZ, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.RET_Z, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.RET_NC, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.RET_C, new OpMetaData() { cycleCount = 2, counterShift = 1 } },
        { OpCodes.RST_0, new OpMetaData() { cycleCount = 4, counterShift = 1 } },
        { OpCodes.RST_1, new OpMetaData() { cycleCount = 4, counterShift = 1 } },
        { OpCodes.RST_2, new OpMetaData() { cycleCount = 4, counterShift = 1 } },
        { OpCodes.RST_3, new OpMetaData() { cycleCount = 4, counterShift = 1 } },
        { OpCodes.RST_4, new OpMetaData() { cycleCount = 4, counterShift = 1 } },
        { OpCodes.RST_5, new OpMetaData() { cycleCount = 4, counterShift = 1 } },
        { OpCodes.RST_6, new OpMetaData() { cycleCount = 4, counterShift = 1 } },
        { OpCodes.RST_7, new OpMetaData() { cycleCount = 4, counterShift = 1 } },
        { OpCodes.DAA, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.CPL, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.NOP, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.HALT, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.STOP, new OpMetaData() { cycleCount = 1, counterShift = 2 } },
        { OpCodes.EI, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
        { OpCodes.DI, new OpMetaData() { cycleCount = 1, counterShift = 1 } },
    };

    //=========================================================================
    // Accessors to combine the paired registers into a 16-bit value for
    // indexing RAM.
    ushort GetBCAddress()
    {
        return (ushort)((B << 8) | C);
    }

    ushort GetDEAddress()
    {
        return (ushort)((D << 8) | E);
    }

    ushort GetHLAddress()
    {
        return (ushort)((H << 8) | L);
    }

    ushort GetFFCAddress()
    {
        return (ushort)(0xFF00 | C);
    }

    ushort GetFFNAddress(byte n)
    {
        return (ushort)(0xFF00 | n);
    }

    void LoadAddressToRegisterPair(ref byte high, ref byte low)
    {
        low = _ram[PC++];
        high = _ram[PC++];
    }

    ushort GetNNAddress()
    {
        return (ushort)(_ram[PC++] | _ram[PC++] << 8);
    }
    
    //=========================================================================
    // Increment and decrement helpers to keep things DRY.
    void Increment(ref byte high, ref byte low)
    {
        if (low == 0xff)
            ++high;
        ++low;
    }

    void Decrement(ref byte high, ref byte low) {
        if (low == 0x00)
            --high;
        --low;
    }

    //=========================================================================
    // Helpers to update the F (flag) register after various operations
    void SetFlagConditional(byte FLAG_MASK, bool test)
	{
		if (test)
		{
            F |= FLAG_MASK;
        }
		else
		{
            F &= (byte)~FLAG_MASK;
        }
	}

    void SetFlag(byte FLAG_MASK) {
        F |= FLAG_MASK;
    }

    void ResetFlag(byte FLAG_MASK) {
        F &= (byte)~FLAG_MASK;
    }

    // Helper enum for updating the subtraction-just-happened flag
    enum MathOperType {
        Addition,
        Subtraction
    }

    // Subtraction is just addition of a negative number; so most of the checks are
    // fundamentally the same. I added a MathOperType because one of the flags is
    // literally "did we just subtract", which is pratty arbitrary but needs to be
    // set regardless.
    void SetFAddition(byte a, byte b, byte result, bool zeroCheck = true)
	{
        var r = a + b;
        var hr = (a & 0xf) + (b & 0xf);

        SetFlagConditional(HALF_CARRY_FLAG, hr > 0x0f);
        SetFlagConditional(CARRY_FLAG, r > 0xff);

        F &= (byte)~SUBT_FLAG;
        if (zeroCheck)
        {
            SetFlagConditional(ZERO_FLAG, result==0);
        }
    }

    void SetFSubtraction(byte a, byte b, byte result, bool zeroCheck = true)
    {
        var r = a - b;
        var hr = (a & 0x0f) - (b & 0x0f);

        SetFlagConditional(HALF_CARRY_FLAG, hr < 0);
        SetFlagConditional(CARRY_FLAG, r < 0);

        SetFlag(SUBT_FLAG);
        if (zeroCheck)
        {
            SetFlagConditional(ZERO_FLAG, result==0);
        }
    }

    void SetFLogic(byte result, bool setHalfCarry)
    {
        F &= (byte)~(SUBT_FLAG | CARRY_FLAG | HALF_CARRY_FLAG);  // unset subtract and carry flags

        if (setHalfCarry)
        {
            F |= HALF_CARRY_FLAG;
        }

		SetFlagConditional(ZERO_FLAG, result==0);
    }

    //=========================================================================
    // Lots of different kinds of addition and subtraction occur, and these
    // helper functions are trying to make it all a bit more DRY across all
    // the dozens of opcodes that use one or the other of these.
    ushort AddByteToUShort(ushort a, byte b)
	{
        ushort result = (ushort)(a + b);
        SetFAddition((byte)(a & 0x00ff), b, (byte)(result & 0x00ff));

        return result;
    }

    byte AddByteToByte(byte a, byte b) {
        byte result = (byte)(a + b);
        SetFAddition(a, b, result);
        return result;
    }

    byte AddByteToAccum(byte b)
	{
        byte result = (byte)(A + b);
        SetFAddition(A, b, result);
		return result;
    }

    byte AddByteAndCarryToAccum(byte b)
    {
        byte carry = (byte)((F & CARRY_FLAG) == CARRY_FLAG ? 1 : 0);
        byte t = (byte)(b + carry);
        byte result = (byte)(A + t);
        SetFAddition(A, t, result);
		return result;
    }

    byte SubtractByteFromAccum(byte b)
    {
        byte result = (byte)(A - b);
        SetFSubtraction(A, b, result);
        return result;
    }

    byte SubtractByteAndCarryFromAccum(byte b)
    {
        byte carry = (byte)((F & CARRY_FLAG) == CARRY_FLAG ? 1 : 0);
        byte n = (byte)(b + carry);
        byte result = (byte)(A - n);
        SetFSubtraction(A, n, result);
        return result;
    }

    void CompareWithAccum(byte b)
    {
        // The compare state is the same as subtract, but with no
        // value returned to be written. So that's thrown out; but
        // the flag values will still be set. So yay!
        SubtractByteAndCarryFromAccum(b);
    }

	void Increment(ref byte b) {
		SetFlagConditional(HALF_CARRY_FLAG, (byte)(b & 0x0f) == 0x0f);
        ResetFlag(SUBT_FLAG);
        SetFlagConditional(ZERO_FLAG, ++b == 0);
    }

	void Decrement(ref byte b)
	{
        SetFlag(SUBT_FLAG);
        SetFlagConditional(HALF_CARRY_FLAG, (byte)(b & 0x0f) == 0x00);
        SetFlagConditional(ZERO_FLAG, --b == 0);
    }

    //=========================================================================
    // Rotation helpers

    // Rotate left through self; but copy the old leftmost bit to carry anyway
    byte Do_RLC(byte value) {
        byte result = (byte)((value << 1) | (value >> 7));

        // 0x80 is binary 1000 0000, checking the leftmost bit for carry
        SetFlagConditional(CARRY_FLAG, (value & 0x80) == 0x80);
        ResetFlag(HALF_CARRY_FLAG);
        ResetFlag(SUBT_FLAG);
        SetFlagConditional(ZERO_FLAG, result == 0);

        return result;
    }

    // Rotate left THROUGH the CARRY_FLAG bit, such that C moves to bit 0 and bit 7 moves to C
    byte Do_RL(byte value) {
        int carryBit = (F & CARRY_FLAG) == CARRY_FLAG ? 1 : 0;

        byte result = (byte)((value << 1) | carryBit);

        // 0x80 is binary 1000 0000, checking the leftmost bit for carry
        SetFlagConditional(CARRY_FLAG, (value & 0x80) == 0x80);
        ResetFlag(HALF_CARRY_FLAG);
        ResetFlag(SUBT_FLAG);
        SetFlagConditional(ZERO_FLAG, result == 0);

        return result;
    }

    // Rotate right through self; but put bit 1 into carry
    byte Do_RRC(byte value) {
        byte result = (byte)((value >> 1) | (value << 7));

        // Set carry flag if 
        SetFlagConditional(CARRY_FLAG, (value & 0x01) == 0x01);
        ResetFlag(HALF_CARRY_FLAG);
        ResetFlag(SUBT_FLAG);
        SetFlagConditional(ZERO_FLAG, result == 0);

        return result;
    }

    // rotate right THROUGH carry
    byte Do_RR(byte value) {
        // 0x80 is binary 1000 0000 (bit 7), so set bit 7 if carry is set
        int carryBit = (F & CARRY_FLAG) == CARRY_FLAG ? 0x80 : 0;

        byte result = (byte)((value >> 1) | carryBit);

        // move bit 1 into the carry flag
        SetFlagConditional(CARRY_FLAG, (value & 0x01) == 0x01);
        ResetFlag(HALF_CARRY_FLAG);
        ResetFlag(SUBT_FLAG);
        SetFlagConditional(ZERO_FLAG, result == 0);

        return result;
    }

    //=========================================================================
    // Shift helpers

    // Shift value to the left one bit
    byte Do_SLA(byte value) {
        // Set carry to the leftmost bit (7), which is binary 1000 0000, or 0x80
        SetFlagConditional(CARRY_FLAG, (byte)(value & 0x80) == 0x80);
        byte result = (byte)(value << 1);
        SetFlagConditional(ZERO_FLAG, result == 0);
        ResetFlag(SUBT_FLAG);
        ResetFlag(HALF_CARRY_FLAG);
        return result;
    }

    // Shift value to the right one bit, preserving bit 7
    byte Do_SRA(byte value) {
        int msb_flag = value & 0x80;
        byte result = (byte)((value >> 1) | msb_flag);

        SetFlagConditional(CARRY_FLAG, (value & 0x01) == 0x01);
        SetFlagConditional(ZERO_FLAG, result == 0);
        ResetFlag(SUBT_FLAG);
        ResetFlag(HALF_CARRY_FLAG);

        return result;
    }

    // Shift value to the right one bit, putting 0 into bit 7
    byte Do_SRL(byte value) {
        // 0x7f is binary 0111_1111 -- a mask to force the leftmost bit to 0
        byte result = (byte)((value >> 1) & 0x7f);

        SetFlagConditional(CARRY_FLAG, (value & 0x01) == 0x01);
        SetFlagConditional(ZERO_FLAG, result == 0);
        ResetFlag(SUBT_FLAG);
        ResetFlag(HALF_CARRY_FLAG);

        return result;
    }

    byte Do_SWAP(byte value) {
        int high = value & 0xf0;
        int low = value & 0x0f;
        int result = high >> 4 | low << 4;


        SetFlagConditional(ZERO_FLAG, result == 0);
        ResetFlag(SUBT_FLAG);
        ResetFlag(HALF_CARRY_FLAG);
        ResetFlag(CARRY_FLAG);

        return (byte)result;
    }

    //=========================================================================
    //
    void Do_JumpConditional(bool test)
    {
        // Always read and increment the PC first, even if
        // the conditional isn't met. ESPECIALLY if not.
        uint low = _ram[PC++];
        uint high = _ram[PC++];
        if (test)
        {
            ushort dest = (ushort)(high << 8 | low);
            //Console.WriteLine("Jump to 0x" + dest.ToString("X4"));
            PC = dest;
            ++_cycles;
        }
    }

    void Do_JumpRelativeConditional(bool test) {
        sbyte relative = (sbyte)_ram[PC++];
        
        if (test)
        {
            ushort dest = (ushort)(PC + relative);
            //Console.WriteLine("Jump (relative) to 0x" + dest.ToString("X4"));
            PC = dest;
            ++_cycles;
        }
    }

    //=========================================================================
    //  Program flow control

    // Push the address from two bytes onto the stack and decrement
    // the stack ponter (SP) appropriately.
    void PushAddressHelper(byte high, byte low) {
        WriteRam(--SP, low);
        WriteRam(--SP, high);
    }

    // Split a ushort into bytes to push onto the stack and
    // decrement the stack pointer (SP) appropriately.
    void PushAddressHelper(ushort address) {
        WriteRam(--SP, (byte)(address & 0x00ff));
        WriteRam(--SP, (byte)((address & 0xff00) >> 8));
    }

    // Pop an address and return the value as a ushort, incrementing
    // the stack pointer (SP) appropriately.
    ushort PopAddressHelper() {
        byte high = _ram[SP++];
        byte low = _ram[SP++];

        return (ushort)((high << 8) | low);
    }

    // Pop an address and write the high and low bytes, incrementing
    // the stack pointer (SP) appropriately.
    void PopAddressHelper(out byte high, out byte low) {
        high = _ram[SP++];
        low = _ram[SP++];
    }

    void Do_CallConditional(bool test)
    {
        byte dest_low = _ram[PC++];
        byte dest_high = _ram[PC++];

		byte current_low = (byte)(PC & 0x00ff);
		byte current_high = (byte)((PC >> 8) & 0x00ff);

        if(test)
        {
			PushAddressHelper(current_high, current_low);

			PC = (ushort)((dest_high << 8) | dest_low);
            _cycles += 3;
        }
    }

    void Do_ReturnConditional(bool test)
    {
        if(test)
        {
            PC = PopAddressHelper();
            _cycles += 3;
        }
    }

    //=========================================================================
    // Helper methods for then the opcode demands a second byte for the operation
    enum SecondOpType : byte {
        ROTATE_SHIFT = 0x00, //00_000_000
        BIT_CHECK = 0x40, //01_000_000
        RESET = 0x80, //10_000_000,
        SET = 0xc0, //11_000_000
    }

    enum SecondOpRegisterPattern
    {
        A = 0x07, //00_000_111
        B = 0x00, //00_000_000
        C = 0x01, //00_000_001
        D = 0x02, //00_000_010
        E = 0x03, //00_000_011
        H = 0x04, //00_000_100
        L = 0x05, //00_000_101
        mHL = 0x06, //00_000_110
    };

    enum SecondOpCode : byte
    {
        RLC = 0x0, //000,
        RL = 0x2, //010,
        RRC = 0x1, //001,
        RR = 0x3, //011,
        SLA = 0x4, //100,
        SRA = 0x5, //101,
        SRL = 0x7, //111,
        SWAP = 0x6, //110,
    };

    byte HandleRotateShiftOp(byte value, SecondOpCode opcode)
    {
        byte result = value;
        switch(opcode) {
            case SecondOpCode.RLC:
                return Do_RLC(value);
            case SecondOpCode.RL:
                return Do_RL(value);
            case SecondOpCode.RRC:
                return Do_RRC(value);
            case SecondOpCode.RR:
                return Do_RR(value);
            case SecondOpCode.SLA:
                return Do_SLA(value);
            case SecondOpCode.SRA:
                return Do_SRA(value);
            case SecondOpCode.SRL:
                return Do_SRL(value);
            case SecondOpCode.SWAP:
                return Do_SWAP(value);
        }

        return result;
    }

    OpMetaData DecodeAndExecute_0xCB()
    {
        OpMetaData metaInfo = new OpMetaData();
        metaInfo.counterShift = 2;
        metaInfo.cycleCount = 2; // most of the ops use 2 cycles

        byte secondOp = _ram[PC++];

        // The register pattern is stored in lowest three bits of the opcode
        int regIdx = (secondOp & 0x7); //0x7 is 00 000 111

        // Most bit operations operate on a single bit; and we can obtain
        // a mask by bytes 3-5 as a number of which bit to use.
        int midBits = (secondOp & 0x38) >> 3; // 0x38 is 00 111 000
        int bitMask = 1 << midBits;

        // This would be much more elegant if I could use pointers to assign
        // registers and pass them along to more generic functions dynamically,
        // however that would require the `unsafe` keyword, which causes issues.
        //
        // TODO: I could use structs or classes for each register to pass a
        // reference into the functions instead of this awful set of switches.
        SecondOpType type = (SecondOpType)(secondOp & 0xc0); //0xc0 is 11 000 000
        switch(type) {
            case SecondOpType.BIT_CHECK:
                // Set the ZERO_FLAG if the specified bit in the register is zero
                switch((SecondOpRegisterPattern)regIdx) {
                    case SecondOpRegisterPattern.A:
                        SetFlagConditional(ZERO_FLAG, (A & bitMask) == 0);
                        break;
                    case SecondOpRegisterPattern.B:
                        SetFlagConditional(ZERO_FLAG, (B & bitMask) == 0);
                        break;
                    case SecondOpRegisterPattern.C:
                        SetFlagConditional(ZERO_FLAG, (C & bitMask) == 0);
                        break;
                    case SecondOpRegisterPattern.D:
                        SetFlagConditional(ZERO_FLAG, (D & bitMask) == 0);
                        break;
                    case SecondOpRegisterPattern.E:
                        SetFlagConditional(ZERO_FLAG, (E & bitMask) == 0);
                        break;
                    case SecondOpRegisterPattern.H:
                        SetFlagConditional(ZERO_FLAG, (H & bitMask) == 0);
                        break;
                    case SecondOpRegisterPattern.L:
                        SetFlagConditional(ZERO_FLAG, (L & bitMask) == 0);
                        break;
                    case SecondOpRegisterPattern.mHL:
                        SetFlagConditional(ZERO_FLAG, (_ram[GetHLAddress()] & bitMask) == 0);
                        metaInfo.cycleCount = 3;
                        break;
                }
                SetFlag(HALF_CARRY_FLAG);
                ResetFlag(SUBT_FLAG);
                break;
            case SecondOpType.SET:
                // do set bit operation
                switch((SecondOpRegisterPattern)regIdx) {
                    case SecondOpRegisterPattern.A:
                        A |= (byte)bitMask;
                        break;
                    case SecondOpRegisterPattern.B:
                        B |= (byte)bitMask;
                        break;
                    case SecondOpRegisterPattern.C:
                        C |= (byte)bitMask;
                        break;
                    case SecondOpRegisterPattern.D:
                        D |= (byte)bitMask;
                        break;
                    case SecondOpRegisterPattern.E:
                        E |= (byte)bitMask;
                        break;
                    case SecondOpRegisterPattern.H:
                        H |= (byte)bitMask;
                        break;
                    case SecondOpRegisterPattern.L:
                        L |= (byte)bitMask;
                        break;
					case SecondOpRegisterPattern.mHL:
						byte value = (byte)(_ram [GetHLAddress ()] | bitMask);
						WriteRam(GetHLAddress(), value);
                        metaInfo.cycleCount = 3;
                        break;
                }
                // No flags are modified
                break;
            case SecondOpType.RESET:
                // do reset bit operation
                switch((SecondOpRegisterPattern)regIdx) {
                    case SecondOpRegisterPattern.A:
                        A &= (byte)~bitMask;
                        break;
                    case SecondOpRegisterPattern.B:
                        B &= (byte)~bitMask;
                        break;
                    case SecondOpRegisterPattern.C:
                        C &= (byte)~bitMask;
                        break;
                    case SecondOpRegisterPattern.D:
                        D &= (byte)~bitMask;
                        break;
                    case SecondOpRegisterPattern.E:
                        E &= (byte)~bitMask;
                        break;
                    case SecondOpRegisterPattern.H:
                        H &= (byte)~bitMask;
                        break;
                    case SecondOpRegisterPattern.L:
                        L &= (byte)~bitMask;
                        break;
					case SecondOpRegisterPattern.mHL:
						byte value = (byte)(_ram[GetHLAddress()] & ~bitMask);
						WriteRam(GetHLAddress(), value);
                        metaInfo.cycleCount = 3;
                        break;
                }
                // No flags are modified
                break;
            case SecondOpType.ROTATE_SHIFT:
                // Find the register for the operation and then
                // pass onto a function which looks for which operation
                // to perform. Could've gone either direction here.
                SecondOpCode code = (SecondOpCode)midBits;
                switch((SecondOpRegisterPattern)regIdx) {
                    case SecondOpRegisterPattern.A:
                        A = HandleRotateShiftOp(A, code);
                        break;
                    case SecondOpRegisterPattern.B:
                        B = HandleRotateShiftOp(B, code);
                        break;
                    case SecondOpRegisterPattern.C:
                        C = HandleRotateShiftOp(C, code);
                        break;
                    case SecondOpRegisterPattern.D:
                        D = HandleRotateShiftOp(D, code);
                        break;
                    case SecondOpRegisterPattern.E:
                        E = HandleRotateShiftOp(E, code);
                        break;
                    case SecondOpRegisterPattern.H:
                        H = HandleRotateShiftOp(H, code);
                        break;
                    case SecondOpRegisterPattern.L:
                        L = HandleRotateShiftOp(L, code);
                        break;
                    case SecondOpRegisterPattern.mHL:
                        WriteRam(GetHLAddress(), HandleRotateShiftOp(_ram[GetHLAddress()], code));
                        metaInfo.cycleCount = 4;
                        break;
                }
                break;
            default:
                throw new System.Exception("Unrecognized second-byte operation!");
        }

        return metaInfo;
    }

    //=========================================================================
    // Correct for binary-coded decimal
    // "Decimal Adjust register A" (DAA)
    // Much of this was borrowed from the equivalent function at:
    // https://github.com/h3nnn4n/here-comes-dat-gameboi/blob/master/src/instructions_arithmetic.c
    void Do_DAA() {
        bool n = (F & SUBT_FLAG) == SUBT_FLAG;

        bool h = (F & HALF_CARRY_FLAG) == HALF_CARRY_FLAG;
        bool c = (F & CARRY_FLAG) == CARRY_FLAG;

        int temp = A;

        if (n)
        {  // SUBTRACTION! :)
            if (h)
            {
                temp = (temp - 0x06) & 0xff;
            }
            if (c)
            {
                temp = temp - 0x60;
            }
        }
        else
        {  // ADDITION! :)
            if (h || (temp & 0x0f) > 9)
            {
                temp += 0x06;
            }

            if (c || temp > 0x9f)
            {
                temp += 0x60;
            }
        }

        A = (byte)temp;
        SetFlagConditional(ZERO_FLAG, A == 0);
        ResetFlag(HALF_CARRY_FLAG);
        SetFlagConditional(CARRY_FLAG, temp > 0xff);
    }

    struct OperationInfo {
        public OpCodes code;
        public ushort addr;
        public byte a;
        public byte b;
        public byte c;
        public byte d;
        public byte e;
        public byte f;
        public byte h;
        public byte l;
    }
    private Queue<OperationInfo> _info_q = new Queue<OperationInfo>();
	private void DumpLastOperations()
	{
		Console.WriteLine("CALL LOG:");
		while(_info_q.Count > 0) {
			OperationInfo info = _info_q.Dequeue();
			Console.WriteLine("0x" + info.addr.ToString("X4") + "  $" + info.code.ToString("X") + " " + info.code.ToString());
		}
	}

	private static ulong _debugCycleCount = 0;
	private static bool _hitJump = false;

    // DEBUG OUTPUT INFO
    private List<OperationInfo> _debugOps = new List<OperationInfo>();

    //=========================================================================
    // Process the next instruction on the CPU
    private void Process(bool printOpcodes)
    {
        if (PC == _ram.Length - 1) {
            Console.WriteLine("Z80CPU hit the end of memory...");
            STOP = true;
            return;
        }

        byte instruction = _ram[PC++];
        OpCodes opcode = (OpCodes)instruction;

        if (!OpInfo.ContainsKey(opcode))
        {
			DumpLastOperations ();
            Console.WriteLine("Unrecognized opcode: " + opcode.ToString("X") + " at address " + (PC-1).ToString("X4") + "; cycles: " + _cycles);
            return;
        }
        else
        {
            if (printOpcodes)
            {
                Console.WriteLine("0x" + (PC - 1).ToString("X4") + "  $" + opcode.ToString("X") + "  " + opcode.ToString());
            }

            OperationInfo info = new OperationInfo() { code = opcode, addr = (ushort)(PC - 1), a=A, b=B, c=C, d=D, e=E, f=F, h=H, l=L };
            _info_q.Enqueue(info);
            while(_info_q.Count > 20)
            {
                _info_q.Dequeue();
            }

            if (_debugOps.Count < 100001)
                _debugOps.Add(info);

            switch(_debugOps.Count)
            {
                case 16963:
                case 40618:
                    _ram[0xFF44] = 0x90;
                    break;
                
                case 17000:
                    _ram[0xFF44] = 0x00;
                    break;
            }
            // if (_debugOps.Count == 42817) {
            //     // _ram[0xFF44] = 0x90;
            //     Console.WriteLine("There's the line number..." + opcode);
            // }

            if (_debugOps.Count == 100000)
            {
                Console.WriteLine("Writing the lines.");
                List<string> lines = new List<string>();
                foreach(var o in _debugOps)
                {
                    lines.Add(String.Format("A:{0} B:{1} C:{2} D:{3} E:{4} F:{5} H:{6} L:{7}",
                                            o.a.ToString("X2"), o.b.ToString("X2"), o.c.ToString("X2"), o.d.ToString("X2"),
                                            o.e.ToString("X2"), o.f.ToString("X2"), o.h.ToString("X2"), o.l.ToString("X2")));
                    lines.Add(String.Format("0x{0}  {1}", o.addr.ToString("X4"), o.code.ToString("X")));
                }

                System.IO.File.WriteAllLines("cs_debug.bytes", lines.ToArray());
                Console.WriteLine("...Done!");
            }
        }


        OpMetaData meta = OpInfo[opcode];
		
 		switch(opcode) {
			case OpCodes.LD_A_A:
                // do nothing since it's copying to itself
                break;
			case OpCodes.LD_A_B:
                A = B;
                break;
			case OpCodes.LD_A_C:
                A = C;
                break;
			case OpCodes.LD_A_D:
                A = D;
                break;
			case OpCodes.LD_A_E:
                A = E;
                break;
			case OpCodes.LD_A_H:
                A = H;
                break;
			case OpCodes.LD_A_L:
                A = L;
                break;
			case OpCodes.LD_B_A:
                B = A;
                break;
			case OpCodes.LD_B_B:
				// pass
				break;
			case OpCodes.LD_B_C:
                B = C;
                break;
			case OpCodes.LD_B_D:
                B = D;
                break;
			case OpCodes.LD_B_E:
                B = E;
                break;
			case OpCodes.LD_B_H:
                B = H;
                break;
			case OpCodes.LD_B_L:
                B = L;
                break;
			case OpCodes.LD_C_A:
                C = A;
                break;
			case OpCodes.LD_C_B:
                C = B;
                break;
			case OpCodes.LD_C_C:
				// pass
				break;
			case OpCodes.LD_C_D:
                C = D;
                break;
			case OpCodes.LD_C_E:
                C = E;
                break;
			case OpCodes.LD_C_H:
                C = H;
                break;
			case OpCodes.LD_C_L:
                C = L;
                break;
			case OpCodes.LD_D_A:
                D = A;
                break;
			case OpCodes.LD_D_B:
                D = B;
                break;
			case OpCodes.LD_D_C:
                D = C;
                break;
			case OpCodes.LD_D_D:
				// pass
				break;
			case OpCodes.LD_D_E:
                D = E;
                break;
			case OpCodes.LD_D_H:
                D = H;
                break;
			case OpCodes.LD_D_L:
                D = L;
                break;
			case OpCodes.LD_E_A:
                E = A;
                break;
			case OpCodes.LD_E_B:
                E = B;
                break;
			case OpCodes.LD_E_C:
                E = C;
                break;
			case OpCodes.LD_E_D:
                E = D;
                break;
			case OpCodes.LD_E_E:
				// pass
				break;
			case OpCodes.LD_E_H:
                E = H;
                break;
			case OpCodes.LD_E_L:
                E = L;
                break;
			case OpCodes.LD_H_A:
                H = A;
                break;
			case OpCodes.LD_H_B:
                H = B;
                break;
			case OpCodes.LD_H_C:
                H = C;
                break;
			case OpCodes.LD_H_D:
                H = D;
                break;
			case OpCodes.LD_H_E:
                H = E;
                break;
			case OpCodes.LD_H_H:
				// pass
				break;
			case OpCodes.LD_H_L:
                H = L;
                break;
			case OpCodes.LD_L_A:
                L = A;
                break;
			case OpCodes.LD_L_B:
                L = B;
                break;
			case OpCodes.LD_L_C:
                L = C;
                break;
			case OpCodes.LD_L_D:
                L = D;
                break;
			case OpCodes.LD_L_E:
                L = E;
                break;
			case OpCodes.LD_L_H:
                L = H;
                break;
			case OpCodes.LD_L_L:
				// pass
				break;
			case OpCodes.LD_A_N:
                A = _ram[PC++];
                break;
			case OpCodes.LD_B_N:
                B = _ram[PC++];
				break;
			case OpCodes.LD_C_N:
                C = _ram[PC++];
				break;
			case OpCodes.LD_D_N:
                D = _ram[PC++];
				break;
			case OpCodes.LD_E_N:
                E = _ram[PC++];
				break;
			case OpCodes.LD_H_N:
                H = _ram[PC++];
				break;
			case OpCodes.LD_L_N:
                L = _ram[PC++];
				break;
			case OpCodes.LD_A_mHL:
                A = _ram[GetHLAddress()];
				break;
			case OpCodes.LD_B_mHL:
                B = _ram[GetHLAddress()];
				break;
			case OpCodes.LD_C_mHL:
                C = _ram[GetHLAddress()];
				break;
			case OpCodes.LD_D_mHL:
                D = _ram[GetHLAddress()];
				break;
			case OpCodes.LD_E_mHL:
                E = _ram[GetHLAddress()];
				break;
			case OpCodes.LD_H_mHL:
                H = _ram[GetHLAddress()];
				break;
			case OpCodes.LD_L_mHL:
                L = _ram[GetHLAddress()];
				break;
			case OpCodes.LD_mHL_A:
                WriteRam(GetHLAddress(), A);
				break;
			case OpCodes.LD_mHL_B:
                WriteRam(GetHLAddress(), B);
				break;
			case OpCodes.LD_mHL_C:
                WriteRam(GetHLAddress(), C);
				break;
			case OpCodes.LD_mHL_D:
                WriteRam(GetHLAddress(), D);
				break;
			case OpCodes.LD_mHL_E:
                WriteRam(GetHLAddress(), E);
				break;
			case OpCodes.LD_mHL_H:
                WriteRam(GetHLAddress(), H);
				break;
			case OpCodes.LD_mHL_L:
                WriteRam(GetHLAddress(), L);
				break;
			case OpCodes.LD_mHL_N:
                WriteRam(GetHLAddress(), _ram[PC++]);
				break;
			case OpCodes.LD_A_mBC:
                A = _ram[GetBCAddress()];
                break;
			case OpCodes.LD_A_mDE:
                A = _ram[GetDEAddress()];
				break;
			case OpCodes.LD_A_mC:
                A = _ram[GetFFCAddress()];
				break;
			case OpCodes.LD_mC_A:
                WriteRam(GetFFCAddress(), A);
				break;
			case OpCodes.LD_A_mN:
                A = _ram[GetFFNAddress(_ram[PC++])];
				break;
			case OpCodes.LD_mN_A:
                WriteRam(GetFFNAddress(_ram[PC++]), A);
				break;
			case OpCodes.LD_A_mNN:
                A = _ram[GetNNAddress()];
				break;
			case OpCodes.LD_mNN_A:
                WriteRam(GetNNAddress(), A);
                break;
			case OpCodes.LD_A_HLI:
                A = _ram[GetHLAddress()];
                Increment(ref H, ref L);
                break;
			case OpCodes.LD_A_HLD:
                A = _ram[GetHLAddress()];
                Decrement(ref H, ref L);
                break;
			case OpCodes.LD_mBC_A:
				WriteRam(GetBCAddress(), A);
				break;
			case OpCodes.LD_mDE_A:
                WriteRam(GetDEAddress(), A);
                break;
			case OpCodes.LD_HLI_A:
                WriteRam(GetHLAddress(), A);
                Increment(ref H, ref L);
                break;
			case OpCodes.LD_HLD_A:
                WriteRam(GetHLAddress(), A);
                Decrement(ref H, ref L);
                break;
			case OpCodes.LD_BC_NN:
                LoadAddressToRegisterPair(ref B, ref C);
                break;
			case OpCodes.LD_DE_NN:
                LoadAddressToRegisterPair(ref D, ref E);
                break;
			case OpCodes.LD_HL_NN:
                LoadAddressToRegisterPair(ref H, ref L);
                break;
			case OpCodes.LD_SP_NN:
                SP = GetNNAddress();
                break;
			case OpCodes.LD_SP_HL:
                SP = GetHLAddress();
                break;
			case OpCodes.PUSH_BC:
                PushAddressHelper(B, C);
                break;
			case OpCodes.PUSH_DE:
                PushAddressHelper(D, E);
				break;
			case OpCodes.PUSH_HL:
                PushAddressHelper(H, L);
				break;
			case OpCodes.PUSH_AF:
                PushAddressHelper(A, F);
				break;
			case OpCodes.POP_BC:
                PopAddressHelper(out B, out C);
                break;
			case OpCodes.POP_DE:
                PopAddressHelper(out D, out E);
				break;
			case OpCodes.POP_HL:
                PopAddressHelper(out H, out L);
				break;
			case OpCodes.POP_AF:
                PopAddressHelper(out A, out F);
				break;
            case OpCodes.LDHL_SP_e:
                {
                    byte b = (byte)_ram[PC++];
                    ushort temp = AddByteToUShort(SP, b);
                    H = (byte)((temp & 0xFF00) >> 8);
                    L = (byte)(temp & 0x00FF);
                }
                break;
            case OpCodes.LD_mNN_SP:
                {
                    ushort addr = GetNNAddress();
                    WriteRam(addr++, (byte)(SP & 0x00ff));
                    WriteRam(_ram[addr], (byte)((SP & 0xff00) >> 8));
                }
                break;
            case OpCodes.ADD_A_A:
                A = AddByteToAccum(A);
                break;
			case OpCodes.ADD_A_B:
                A = AddByteToAccum(B);
				break;
			case OpCodes.ADD_A_C:
                A = AddByteToAccum(C);
				break;
			case OpCodes.ADD_A_D:
                A = AddByteToAccum(D);
				break;
			case OpCodes.ADD_A_E:
                A = AddByteToAccum(E);
				break;
			case OpCodes.ADD_A_H:
                A = AddByteToAccum(H);
				break;
			case OpCodes.ADD_A_L:
                A = AddByteToAccum(L);
				break;
			case OpCodes.ADD_A_N:
                A = AddByteToAccum(_ram[PC++]);
				break;
			case OpCodes.ADD_A_mHL:
                A = AddByteToAccum(_ram[GetHLAddress()]);
				break;
			case OpCodes.ADC_A_A:
                A = AddByteAndCarryToAccum(A);
                break;
			case OpCodes.ADC_A_B:
                A = AddByteAndCarryToAccum(B);
				break;
			case OpCodes.ADC_A_C:
                A = AddByteAndCarryToAccum(C);
				break;
			case OpCodes.ADC_A_D:
                A = AddByteAndCarryToAccum(D);
				break;
			case OpCodes.ADC_A_E:
                A = AddByteAndCarryToAccum(E);
				break;
			case OpCodes.ADC_A_H:
                A = AddByteAndCarryToAccum(H);
				break;
			case OpCodes.ADC_A_L:
                A = AddByteAndCarryToAccum(L);
				break;
			case OpCodes.ADC_A_N:
                A = AddByteAndCarryToAccum(_ram[PC++]);
				break;
			case OpCodes.ADC_A_mHL:
                A = AddByteAndCarryToAccum(_ram[GetHLAddress()]);
				break;
			case OpCodes.SUB_A:
                A = SubtractByteFromAccum(A);
                break;
			case OpCodes.SUB_B:
                A = SubtractByteFromAccum(B);
				break;
			case OpCodes.SUB_C:
                A = SubtractByteFromAccum(C);
				break;
			case OpCodes.SUB_D:
                A = SubtractByteFromAccum(D);
				break;
			case OpCodes.SUB_E:
                A = SubtractByteFromAccum(E);
				break;
			case OpCodes.SUB_H:
                A = SubtractByteFromAccum(H);
				break;
			case OpCodes.SUB_L:
                A = SubtractByteFromAccum(L);
				break;
			case OpCodes.SUB_N:
                A = SubtractByteFromAccum(_ram[PC++]);
				break;
			case OpCodes.SUB_mHL:
                A = SubtractByteFromAccum(_ram[GetHLAddress()]);
				break;
			case OpCodes.SBC_A_A:
                A = SubtractByteAndCarryFromAccum(A);
                break;
			case OpCodes.SBC_A_B:
                A = SubtractByteAndCarryFromAccum(B);
				break;
			case OpCodes.SBC_A_C:
                A = SubtractByteAndCarryFromAccum(C);
				break;
			case OpCodes.SBC_A_D:
                A = SubtractByteAndCarryFromAccum(D);
				break;
			case OpCodes.SBC_A_E:
                A = SubtractByteAndCarryFromAccum(E);
				break;
			case OpCodes.SBC_A_H:
                A = SubtractByteAndCarryFromAccum(H);
				break;
			case OpCodes.SBC_A_L:
                A = SubtractByteAndCarryFromAccum(L);
				break;
			case OpCodes.SBC_A_N:
                A = SubtractByteAndCarryFromAccum(_ram[PC++]);
				break;
			case OpCodes.SBC_A_mHL:
                A = SubtractByteAndCarryFromAccum(_ram[GetHLAddress()]);
				break;
			case OpCodes.AND_A:
                A = (byte)(A & A);
                SetFLogic(A, true);
                break;
			case OpCodes.AND_B:
                A = (byte)(A & B);
                SetFLogic(A, true);
				break;
			case OpCodes.AND_C:
                A = (byte)(A & C);
                SetFLogic(A, true);
				break;
			case OpCodes.AND_D:
                A = (byte)(A & D);
                SetFLogic(A, true);
				break;
			case OpCodes.AND_E:
                A = (byte)(A & E);
                SetFLogic(A, true);
				break;
			case OpCodes.AND_H:
                A = (byte)(A & H);
                SetFLogic(A, true);
				break;
			case OpCodes.AND_L:
                A = (byte)(A & L);
                SetFLogic(A, true);
				break;
			case OpCodes.AND_N:
                A = (byte)(A & _ram[PC++]);
                SetFLogic(A, true);
				break;
			case OpCodes.AND_mHL:
                A = (byte)(A & _ram[GetHLAddress()]);
                SetFLogic(A, true);
				break;
			case OpCodes.OR_A:
                A = (byte)(A | A);
                SetFLogic(A, false);
				break;
			case OpCodes.OR_B:
                A = (byte)(A | B);
                SetFLogic(A, false);
				break;
			case OpCodes.OR_C:
                A = (byte)(A | C);
                SetFLogic(A, false);
				break;
			case OpCodes.OR_D:
                A = (byte)(A | D);
                SetFLogic(A, false);
				break;
			case OpCodes.OR_E:
                A = (byte)(A | E);
                SetFLogic(A, false);
				break;
			case OpCodes.OR_H:
                A = (byte)(A | H);
                SetFLogic(A, false);
				break;
			case OpCodes.OR_L:
                A = (byte)(A | L);
                SetFLogic(A, false);
				break;
			case OpCodes.OR_N:
                A = (byte)(A | _ram[PC++]);
                SetFLogic(A, false);
				break;
			case OpCodes.OR_mHL:
                A = (byte)(A | _ram[GetHLAddress()]);
                SetFLogic(A, false);
				break;
			case OpCodes.XOR_A:
                A ^= A;
                SetFLogic(A, false);
				break;
			case OpCodes.XOR_B:
                A ^= B;
                SetFLogic(A, false);
				break;
			case OpCodes.XOR_C:
                A ^= C;
                SetFLogic(A, false);
				break;
			case OpCodes.XOR_D:
                A ^= D;
                SetFLogic(A, false);
				break;
			case OpCodes.XOR_E:
                A ^= E;
                SetFLogic(A, false);
				break;
			case OpCodes.XOR_H:
                A ^= H;
                SetFLogic(A, false);
				break;
			case OpCodes.XOR_L:
                A ^= L;
                SetFLogic(A, false);
				break;
			case OpCodes.XOR_N:
                A ^= _ram[PC++];
                SetFLogic(A, false);
				break;
			case OpCodes.XOR_mHL:
                A ^= _ram[GetHLAddress()];
                SetFLogic(A, false);
				break;
			case OpCodes.CP_A:
                CompareWithAccum(A);
                break;
			case OpCodes.CP_B:
                CompareWithAccum(B);
				break;
			case OpCodes.CP_C:
                CompareWithAccum(C);
				break;
			case OpCodes.CP_D:
                CompareWithAccum(D);
				break;
			case OpCodes.CP_E:
                CompareWithAccum(E);
				break;
			case OpCodes.CP_H:
                CompareWithAccum(H);
				break;
			case OpCodes.CP_L:
                CompareWithAccum(L);
				break;
			case OpCodes.CP_N:
                CompareWithAccum(_ram[PC++]);
				break;
			case OpCodes.CP_mHL:
                CompareWithAccum(_ram[GetHLAddress()]);
				break;
			case OpCodes.INC_A:
                Increment(ref A);
                break;
			case OpCodes.INC_B:
                Increment(ref B);
				break;
			case OpCodes.INC_C:
                Increment(ref C);
				break;
			case OpCodes.INC_D:
                Increment(ref D);
				break;
			case OpCodes.INC_E:
                Increment(ref E);
				break;
			case OpCodes.INC_H:
                Increment(ref H);
				break;
			case OpCodes.INC_L:
                Increment(ref L);
				break;
			case OpCodes.INC_mHL:
                Increment(ref _ram[GetHLAddress()]);
				break;
			case OpCodes.DEC_A:
                Decrement(ref A);
				break;
			case OpCodes.DEC_B:
                Decrement(ref B);
				break;
			case OpCodes.DEC_C:
                Decrement(ref C);
				break;
			case OpCodes.DEC_D:
                Decrement(ref D);
				break;
			case OpCodes.DEC_E:
                Decrement(ref E);
				break;
			case OpCodes.DEC_H:
                Decrement(ref H);
				break;
			case OpCodes.DEC_L:
                Decrement(ref L);
				break;
			case OpCodes.DEC_mHL:
                Decrement(ref _ram[GetHLAddress()]);
				break;
			case OpCodes.ADD_HL_BC:
                L = AddByteToByte(L, C);
                {
                    int carry = (byte)(F & CARRY_FLAG) == CARRY_FLAG ? 1 : 0;
                    H = AddByteToByte(H, (byte)(B + carry));
                }
                break;
			case OpCodes.ADD_HL_DE:
                L = AddByteToByte(L, E);
                {
                    int carry = (byte)(F & CARRY_FLAG) == CARRY_FLAG ? 1 : 0;
                    H = AddByteToByte(H, (byte)(D + carry));
                }
				break;
			case OpCodes.ADD_HL_HL:
                L = AddByteToByte(L, L);
                {
                    int carry = (byte)(F & CARRY_FLAG) == CARRY_FLAG ? 1 : 0;
                    H = AddByteToByte(H, (byte)(H + carry));
                }
				break;
			case OpCodes.ADD_HL_SP:
                L = AddByteToByte(L, (byte)(SP & 0x00FF));
                {
                    int carry = (byte)(F & CARRY_FLAG) == CARRY_FLAG ? 1 : 0;
                    H = AddByteToByte(H, (byte)((SP & 0xFF00) >> 8 + carry));
                }
				break;
			case OpCodes.ADD_SP_e:
                SP = AddByteToUShort(SP, _ram[PC++]);
                break;
			case OpCodes.INC_BC:
                Increment(ref B, ref C);
                break;
			case OpCodes.INC_DE:
                Increment(ref D, ref E);
				break;
			case OpCodes.INC_HL:
                Increment(ref H, ref L);
				break;
            case OpCodes.INC_SP:
                {
                    // TODO: is there maybe a more efficient way to handle this?
                    byte sp_low = (byte)(SP & 0x00ff);
                    byte sp_high = (byte)((SP & 0xff00) >> 8);
                    Increment(ref sp_high, ref sp_low);
                    SP = (ushort)(sp_low | (sp_high << 8));
                }
                break;
            case OpCodes.DEC_BC:
                Decrement(ref B, ref C);
                break;
			case OpCodes.DEC_DE:
                Decrement(ref D, ref E);
				break;
			case OpCodes.DEC_HL:
                Decrement(ref H, ref L);
				break;
			case OpCodes.DEC_SP:
                {
                    // TODO: is there maybe a more efficient way to handle this?
                    byte sp_low = (byte)(SP & 0x00ff);
                    byte sp_high = (byte)((SP & 0xff00) >> 8);
                    Decrement(ref sp_high, ref sp_low);
                    SP = (ushort)(sp_low | (sp_high << 8));
                }
				break;
			case OpCodes.RLCA:
                A = Do_RLC(A);
				break;
			case OpCodes.RLA:
                A = Do_RL(A);
				break;
			case OpCodes.RRCA:
                A = Do_RRC(A);
				break;
			case OpCodes.RRA:
                A = Do_RR(A);
				break;
            case OpCodes.MULTI_BYTE_OP:	// this code accounts for many variants based on the second byte read
                {
                    OpMetaData data = DecodeAndExecute_0xCB();
                    _cycles += data.cycleCount;
                }
                break;
            case OpCodes.JP_NN:
                Do_JumpConditional(true);
                break;
			case OpCodes.JP_NZ_NN:
                Do_JumpConditional((F & ZERO_FLAG) == 0);
                break;
			case OpCodes.JP_Z_NN:
                Do_JumpConditional((F & ZERO_FLAG) == ZERO_FLAG);
				break;
			case OpCodes.JP_NC_NN:
                Do_JumpConditional((F & CARRY_FLAG) == 0);
				break;
			case OpCodes.JP_C_NN:
                Do_JumpConditional((F & CARRY_FLAG) == CARRY_FLAG);
				break;
			case OpCodes.JR_e:
                Do_JumpRelativeConditional(true);
                break;
			case OpCodes.JR_NZ_e:
                Do_JumpRelativeConditional((F & ZERO_FLAG) == 0);
				break;
			case OpCodes.JR_Z_e:
                Do_JumpRelativeConditional((F & ZERO_FLAG) == ZERO_FLAG);
				break;
			case OpCodes.JR_NC_e:
                Do_JumpRelativeConditional((F & CARRY_FLAG) == 0);
				break;
			case OpCodes.JR_C_e:
                Do_JumpRelativeConditional((F & CARRY_FLAG) == CARRY_FLAG);
				break;
			case OpCodes.JP_mHL:  // Actually just loads HL into PC, not memory at HL... :(
                PC = GetHLAddress();
                break;
			case OpCodes.CALL_NN:
                Do_CallConditional(true);
                break;
			case OpCodes.CALL_NZ_NN:
                Do_CallConditional((F & ZERO_FLAG) == 0);
				break;
			case OpCodes.CALL_Z_NN:
                Do_CallConditional((F & ZERO_FLAG) == ZERO_FLAG);
				break;
			case OpCodes.CALL_NC_NN:
                Do_CallConditional((F & CARRY_FLAG) == 0);
				break;
			case OpCodes.CALL_C_NN:
                Do_CallConditional((F & CARRY_FLAG) == CARRY_FLAG);
				break;
			case OpCodes.RET:
                Do_ReturnConditional(true);
                break;
			case OpCodes.RETI:
                Do_ReturnConditional(true);
                IME = true;
                break;
			case OpCodes.RET_NZ:
                Do_ReturnConditional((F & ZERO_FLAG) == 0);
				break;
			case OpCodes.RET_Z:
                Do_ReturnConditional((F & ZERO_FLAG) == ZERO_FLAG);
				break;
			case OpCodes.RET_NC:
                Do_ReturnConditional((F & CARRY_FLAG) == 0);
				break;
			case OpCodes.RET_C:
                Do_ReturnConditional((F & CARRY_FLAG) == CARRY_FLAG);
				break;
			case OpCodes.RST_0:
                PushAddressHelper(PC);
                PC = 0x0000;
                break;
			case OpCodes.RST_1:
                PushAddressHelper(PC);
                PC = 0x0008;
				break;
			case OpCodes.RST_2:
                PushAddressHelper(PC);
                PC = 0x0010;
				break;
			case OpCodes.RST_3:
                PushAddressHelper(PC);
                PC = 0x0018;
				break;
			case OpCodes.RST_4:
                PushAddressHelper(PC);
                PC = 0x0020;
				break;
			case OpCodes.RST_5:
                PushAddressHelper(PC);
                PC = 0x0028;
				break;
			case OpCodes.RST_6:
                PushAddressHelper(PC);
                PC = 0x0030;
				break;
			case OpCodes.RST_7:
                PushAddressHelper(PC);
                PC = 0x0038;
				break;
			case OpCodes.DAA:
                Do_DAA();
                break;
			case OpCodes.CPL:
                A = (byte)~A;
                break;
			case OpCodes.NOP:
                // literally no operation done here
				break;
			case OpCodes.HALT:
                HALT = true;
                break;
			case OpCodes.STOP:
                WriteRam(IE_Addr, 0);
                // TODO: set all inputs to LOW
                STOP = true;
                break;
			case OpCodes.EI:
                IME = true;
                break;
			case OpCodes.DI:
                IME = false;
                break;
        }
        _cycles += meta.cycleCount;
    } // end Process() method

} // end Z80CPU class
