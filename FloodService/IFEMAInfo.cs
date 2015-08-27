using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace FloodService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IFEMAInfo" in both code and config file together.
    [ServiceContract]
    public interface IFEMAInfo
    {
        [OperationContract]
        Stream GetFEMAInfo(string address);
    }
}
