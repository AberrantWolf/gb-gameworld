using System;

class GBHeader {
    public enum SGBFunctions : byte
    {
        GB_Only = 0x0,
        SGB_Okay = 0x3,
        ERROR
    }
    public enum GBCVal : byte
    {
        Other = 0x0,
        Color = 0x80,
        ERROR
    }
    public enum CartridgeType : ushort
    {
        ROM_ONLY = 0x0,
        ROM_MBC1 = 0x1,
        ROM_MBC1_RAM = 0x2,
        ROM_MBC1_RAM_BATT = 0x3,
        ROM_MBC2 = 0x5,
        ROM_MBC2_BATTERY = 0x6,
        ROM_RAM = 0x8,
        ROM_RAM_BATTERY = 0x9,
        ROM_MMM01 = 0xB,
        ROM_MMM01_SRAM = 0xC,
        ROM_MMM01_SRAM_BATT = 0xD,
        ROM_MBC3_TIMER_BATT = 0xF,
        ROM_MBC3_TIMER_RAM_BATT = 0x10,
        ROM_MBC3 = 0x11,
        ROM_MBC3_RAM = 0x12,
        ROM_MBC3_RAM_BATT = 0x13,
        ROM_MBC5 = 0x19,
        ROM_MBC5_RAM = 0x1A,
        ROM_MBC5_RAM_BATT = 0x1B,
        ROM_MBC5_RUMBLE = 0x1C,
        ROM_MBC5_RUMBLE_SRAM = 0x1D,
        ROM_MBC5_RUMBLE_SRAM_BATT = 0x1E,
        Pocket_Camera = 0x1F,
        Bandai_TAMA5 = 0xFD,
        Hudson_HuC_3 = 0xFE,
        Hudson_HuC_1 = 0xFF,
        ERROR
    }
    public enum ROMSize : byte
    {
        BANKS_2 = 0,
        BANKS_4 = 1,
        BANKS_8 = 2,
        BANKS_16 = 3,
        BANKS_32 = 4,
        BANKS_64 = 5,
        BANKS_128 = 6,
        BANKS_72 = 0x52,
        BANKS_80 = 0x53,
        BANKS_96 = 0x54,
        ERROR
    }
    public enum RAMSize : byte
    {
        None = 0,
        BANKS_1_2kB = 1,
        BANKS_1_8kB = 2,
        BANKS_4 = 3,
        BANKS_16 = 4 ,
        ERROR
    }
    public enum DestinationCode : byte
    {
        Japanese = 0x0,
        Non_Japanese = 0x1,
        ERROR
    }
    public enum OldLicenseeCode : byte
    {
        CheckNewCode = 0x33,
        Accolade = 0x79,
        Konami = 0xA4,
        ERROR,
    }

    private byte[] _scrollingGraphic = {
        0xCE,0xED,0x66,0x66,0xCC,0x0D,0x00,0x0B,
        0x03,0x73,0x00,0x83,0x00,0x0C,0x00,0x0D,
        0x00,0x08,0x11,0x1F,0x88,0x89,0x00,0x0E,
        0xDC,0xCC,0x6E,0xE6,0xDD,0xDD,0xD9,0x99,
        0xBB,0xBB,0x67,0x63,0x6E,0x0E,0xEC,0xCC,
        0xDD,0xDC,0x99,0x9F,0xBB,0xB9,0x33,0x3E
    };

    private bool CheckScrollingGraphics(byte[] rom)
    {
        for (int i = 0; i < 0x30; i++)
        {
            if (rom[0x104 + i] != _scrollingGraphic[i])
            {
                return false;
            }
        }

        return true;
    }

    private string GetTitle(byte[] rom)
    {
        byte[] name = new byte[16];
        Array.Copy(rom, 0x134, name, 0, 16);

        return System.Text.Encoding.ASCII.GetString(name).TrimEnd('\0');
    }

    private GBCVal GetGBCVal(byte[] rom)
    {
        byte val = rom[0x143];
        if (Enum.IsDefined(typeof(GBCVal), val))
        {
            return (GBCVal)val;
        }

        return GBCVal.ERROR;
    }

    private byte GetNewLicenseeCode(byte[] rom)
    {
        int result = 0;
        result |= (rom[0x144] & 0xF) << 4;
        result |= (rom[0x145] & 0xF);

        return (byte)result;
    }

    private SGBFunctions GetSGBFunctions(byte[] rom)
    {
        byte val = rom[0x146];
        if (Enum.IsDefined(typeof(SGBFunctions), val))
        {
            return (SGBFunctions)val;
        }

        return SGBFunctions.ERROR;
    }

    private CartridgeType GetCartridgeType(byte[] rom)
    {
        ushort val = rom[0x147];
        if (Enum.IsDefined(typeof(CartridgeType), val))
        {
            return (CartridgeType)val;
        }

        return CartridgeType.ERROR;
    }

    private ROMSize GetRomSize(byte[] rom)
    {
        byte val = rom[0x148];
        if (Enum.IsDefined(typeof(ROMSize), val))
        {
            return (ROMSize)val;
        }

        return ROMSize.ERROR;
    }

    private RAMSize GetRamSize(byte[] rom)
    {
        byte val = rom[0x149];
        if (Enum.IsDefined(typeof(RAMSize), val))
        {
            return (RAMSize)val;
        }

        return RAMSize.ERROR;
    }

    private DestinationCode GetDestinationCode(byte[] rom)
    {
        byte val = rom[0x14A];
        if (Enum.IsDefined(typeof(DestinationCode), val))
        {
            return (DestinationCode)val;
        }

        return DestinationCode.ERROR;
    }

    private OldLicenseeCode GetOldLicenseeCode(byte[] rom)
    {
        byte val = rom[0x14B];
        if (Enum.IsDefined(typeof(OldLicenseeCode), val))
        {
            return (OldLicenseeCode)val;
        }

        Console.WriteLine("Old lincesee code error: {0:X2}", val);
        return OldLicenseeCode.ERROR;
    }

    private bool RunComplementCheck(byte[] rom)
    {
        int sum = 0;
        for (int i = 0x134; i < 0x14E; ++i)
        {
            sum += rom[i];
        }
        sum += 25;

        return (sum & 0xFF) == 0x0;
    }

    public readonly bool couldParse = false;
    public readonly bool isValid = false;
    public readonly bool containsScrollingNintendoGraphic = false;
    //private byte[] gameTitle = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    private string gameTitle;
    public readonly GBCVal gbcVal;
    public readonly byte newLicenseeCode;
    public readonly SGBFunctions sgbFunctions;
    public readonly CartridgeType cartridgeType;
    public readonly ROMSize romSize;
    public readonly RAMSize ramSize;
    public readonly DestinationCode destinationCode;
    public readonly OldLicenseeCode oldLicenseeCode;
    public readonly byte maskROMVersion;
    public readonly byte complementCheckByte;
    public readonly bool passesComplementCheck;
    public readonly byte checksumHigh;
    public readonly byte checksumLow;

    public GBHeader(byte[] rom)
    {
        // Verify that there's at least enough here to try reading the header info
        if (rom.Length < 0x014f)
        {
            return;
        }
        couldParse = true;

        containsScrollingNintendoGraphic = CheckScrollingGraphics(rom);
        gameTitle = GetTitle(rom);
        gbcVal = GetGBCVal(rom);
        newLicenseeCode = GetNewLicenseeCode(rom);
        sgbFunctions = GetSGBFunctions(rom);
        cartridgeType = GetCartridgeType(rom);
        romSize = GetRomSize(rom);
        ramSize = GetRamSize(rom);
        destinationCode = GetDestinationCode(rom);
        oldLicenseeCode = GetOldLicenseeCode(rom);
        maskROMVersion = rom[0x14C];
        complementCheckByte = rom[0x14D];
        checksumHigh = rom[0x14E];
        checksumLow = rom[0x14F];

        passesComplementCheck = RunComplementCheck(rom);

        isValid = containsScrollingNintendoGraphic & passesComplementCheck;
    }

    public void PrintStats()
    {
        Console.WriteLine("Nintendo Graphic: {0}", containsScrollingNintendoGraphic);
        Console.WriteLine("Game Title: {0}", gameTitle);
        Console.WriteLine("GBC Value: {0}", gbcVal);
        Console.WriteLine("New Licensee Code: ${0:X2}", newLicenseeCode);
        Console.WriteLine("SGB Functions: {0}", sgbFunctions);
        Console.WriteLine("Cartridge Type: {0}", cartridgeType);
        Console.WriteLine("ROM Size: {0}", romSize);
        Console.WriteLine("RAM Size: {0}", ramSize);
        Console.WriteLine("Destination Code: {0}", destinationCode);
        Console.WriteLine("Old Licensee Code: {0}", oldLicenseeCode);
        Console.WriteLine("Mask ROM Version: {0}", maskROMVersion);
        Console.WriteLine("Complement Check Byte: ${0:X2}", complementCheckByte);
        Console.WriteLine("Checksum High Byte: {0}", checksumHigh);
        Console.WriteLine("Checksum Low Byte: {0}", checksumLow);
        Console.WriteLine("Did Pass Complement?: {0}", passesComplementCheck);
        Console.WriteLine("Is Valid?: {0}", isValid);
    }
}