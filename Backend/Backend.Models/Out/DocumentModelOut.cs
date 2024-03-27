namespace Backend.Models.Out
{
    /// <summary>
    /// Represents a document to be returned
    /// </summary>
    public class DocumentModelOut
    {
        /// <summary>
        /// Name of the document
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Status of the document
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// URL of the document
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Field to store the serialized JSON
        /// </summary>
        public string Fields { get; set; }
    }
}
