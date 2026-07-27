using System;

[Flags]
public enum PipeDir
{
    None = 0,
    Top = 1 << 0,
    Right = 1 << 1,
    Bottom = 1 << 2,
    Left = 1 << 3
}
public enum PipeType
{
    Empty, 
    Start, 
    End, 
    Pipe_1_Straight, 
    Pipe_1_Corner, 
    Pipe_2_T, 
    Pipe_3_Cross
}

[Serializable]
public struct PipeData
{
    public PipeType type;
    public int rotationIndex;

    public PipeDir GetCurrentDirections()
    {
        PipeDir baseDir = type switch
        {
            PipeType.Start => PipeDir.Top,
            PipeType.End => PipeDir.Top,
            PipeType.Pipe_1_Straight => PipeDir.Top | PipeDir.Bottom,
            PipeType.Pipe_1_Corner => PipeDir.Left | PipeDir.Bottom,
            PipeType.Pipe_2_T => PipeDir.Top | PipeDir.Left | PipeDir.Right,
            PipeType.Pipe_3_Cross => PipeDir.Top | PipeDir.Bottom | PipeDir.Left | PipeDir.Right,
            _ => PipeDir.None
        };

        int dirVal = (int)baseDir;
        for (int i = 0; i < rotationIndex; i++)
        {
            dirVal = ((dirVal << 1) | (dirVal >> 3)) & 0x0F;
        }

        return (PipeDir)dirVal;
    }
}
