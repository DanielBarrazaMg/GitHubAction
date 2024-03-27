namespace Backend.Entities
{

    /// <summary>
    /// Represents a document
    /// </summary>
    public class Document : BaseEntity
    {
        /// <summary>
        /// Name of the document
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Status of the document
        /// </summary>
        public DocumentStatus Status { get; set; }

        /// <summary>
        /// URL of the document
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Field to store the serialized JSON
        /// </summary>
        public string Fields { get; set; }
    }

    public enum DocumentStatus
    {
        Pending,
        Processed
    }
}
