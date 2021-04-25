using System;
using System.Collections.Generic;
using System.Text;

namespace AMZN.Model
{
    /// <summary>
    /// A tábla mezőinek típusai
    /// </summary>
    public enum Types
    {
        EMPTY,
        ROBOT,
        STATION,
        POD,
        DOCKER,
        ROBOT_UNDER_POD,
        ROBOT_WITH_POD,
        ROBOT_ON_STATION,
        ROBOT_WITH_POD_ON_STATION,
        ROBOT_ON_DOCKER,
        NULL,
        SELECTED,
        MOVING
    };
}
