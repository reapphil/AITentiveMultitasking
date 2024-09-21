using System;

namespace Utils {

    public enum SignState {
        None = 0,
        Free = 1,
        Blocked = 2,
    }
    
    
    public enum Lane {
        Left = 0,
        Center = 1,
        Right = 2,
    }
    
    public enum InfoLineType {
        TargetSpeedChange = 0,
        LaneChangeStart = 1,
        LaneChangeEnd = 2,
        LaneChangeFailed = 3,
        OptimalLaneChangeStart = 4,
        OptimalLaneChangeEnd = 5,
    }

    
}

