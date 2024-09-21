using System;

namespace Utils {
    public class EnumHelper {
        
        public static T GetRandomEnumValue<T>() where T : Enum {
            Random random = new Random();
            var enumValues = Enum.GetValues(typeof(T));
            int randomIndex = random.Next(enumValues.Length);
            return (T)enumValues.GetValue(randomIndex);
        }
        
    }
}