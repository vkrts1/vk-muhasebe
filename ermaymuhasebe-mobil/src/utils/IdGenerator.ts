let lastGeneratedId = 0;

/**
 * Generates an ID that fits within a 32-bit signed integer (max 2,147,483,647).
 * This prevents OverflowException on the C# backend when mapping Firebase JSON to SQLite Entities.
 */
export const generateInt32Id = (): number => {
    // Generate seconds since epoch: ~1,723,000,000
    let newId = Math.floor(Date.now() / 1000);
    
    // Ensure monotonicity if called multiple times in the same second
    if (newId <= lastGeneratedId) {
        newId = lastGeneratedId + 1;
    }
    
    lastGeneratedId = newId;
    return newId;
};
