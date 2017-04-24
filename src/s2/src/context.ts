interface s2Context{
    Token: string;
    Directory: string;
    JsonConfig: string;
    Messages: DacModelError[];
}

interface DacModelError{
    ErrorCode: number;
    Line: number;
    Column: number;
    Prefix: string;
    Message: string;
    SourceName: string;
}

//  public ModelErrorType ErrorType { get; }
//     /// <summary>DacModelError error code</summary>
//     public int ErrorCode { get; }
//     /// <summary>Line Number of the error</summary>
//     public int Line { get; }
//     /// <summary>Column Number of the error</summary>
//     public int Column { get; }
//     /// <summary>DacModelError prefix</summary>
//     public string Prefix { get; }
//     /// <summary>Message from DacModelError</summary>
//     public string Message { get; }
//     /// <summary>
//     /// The TSqlObject with error.
//     /// Can be null if the object creation failed completely.
//     /// Could be a partially constructed object in case of partial failures in object creation.
//     /// </summary>
//     public string SourceName { get; }
//   }