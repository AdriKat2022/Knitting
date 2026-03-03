namespace Knitting.Interfaces
{
    public interface IVariableContext
    {
        /// <summary>
        /// Initializes the variable context.
        /// </summary>
        public void Initialize();
        
        /// <summary>
        /// Returns whether if the variable exists in the context or not.
        /// </summary>
        public bool Contains(string variableName);
        
        /// <summary>
        /// Gets the value of the given variable name.
        /// </summary>
        public string this[string variableName] { get; set; }
        
        public string GetValueOrDefault(string variableName) => Contains(variableName) ? this[variableName] : null;
    }
}