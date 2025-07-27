// --- START OF FILE GenericParameterDrawer.cs ---

using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom PropertyDrawer cho lớp GenericParameter và tất cả các lớp kế thừa từ nó.
/// Drawer này chịu trách nhiệm vẽ giao diện người dùng (UI) cho một tham số,
/// cho phép người dùng chọn giữa việc sử dụng một giá trị hằng (constant) hoặc
/// một giá trị động được lấy từ một component khác.
/// </summary>
[CustomPropertyDrawer(typeof(GenericParameter), true)]
public class GenericParameterDrawer : PropertyDrawer
{
    // Hằng số để quản lý giao diện
    private const float FOLD_OUT_WIDTH = 15f;

    /// <summary>
    /// Hàm này được gọi để vẽ giao diện của property trong Inspector.
    /// </summary>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Lấy các property con từ SerializedProperty chính
        var useConstantProp = property.FindPropertyRelative("useConstant");
        var constantValueProp = property.FindPropertyRelative("constantValue");
        var sourceComponentProp = property.FindPropertyRelative("sourceComponent");
        var sourceFieldNameProp = property.FindPropertyRelative("sourceFieldName");

        // === Kiểm tra an toàn ===
        // Nếu không tìm thấy property con (có thể xảy ra trong quá trình Unity cập nhật),
        // hiển thị thông báo lỗi và thoát để tránh NullReferenceException.
        if (useConstantProp == null)
        {
            EditorGUI.LabelField(position, label.text, "Error: Parameter properties not found.");
            EditorGUI.EndProperty();
            return;
        }

        // Khai báo các biến kích thước và vị trí
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        
        // Vẽ Foldout (mũi tên xổ xuống) để người dùng có thể thu gọn/mở rộng giao diện của tham số
        Rect foldoutRect = new Rect(position.x, position.y, FOLD_OUT_WIDTH, lineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, true);
        
        // Vẽ label của parameter (ví dụ: "soundName", "baseDamage") ngay bên cạnh foldout
        Rect labelRect = new Rect(position.x + FOLD_OUT_WIDTH, position.y, position.width - FOLD_OUT_WIDTH, lineHeight);
        EditorGUI.LabelField(labelRect, label);
        
        // Nếu người dùng mở rộng foldout, chúng ta sẽ vẽ các trường chi tiết
        if (property.isExpanded)
        {
            // Bắt đầu một khối thụt vào để các trường con nằm gọn gàng bên dưới label
            EditorGUI.indentLevel++;

            float currentY = position.y + lineHeight + spacing;
            Rect contentRect = new Rect(position.x, currentY, position.width, lineHeight);
            
            // Vẽ Toggle "Use Constant"
            EditorGUI.PropertyField(contentRect, useConstantProp);
            currentY += lineHeight + spacing;

            // Dựa vào giá trị của "Use Constant", hiển thị giao diện tương ứng
            if (useConstantProp.boolValue)
            {
                // -- Chế độ giá trị hằng --
                // Kiểm tra xem property constantValue có tồn tại không
                if (constantValueProp != null)
                {
                    // Lấy chiều cao cần thiết cho trường constantValue (có thể nhiều dòng nếu là struct)
                    float valueHeight = EditorGUI.GetPropertyHeight(constantValueProp, true);
                    Rect valueRect = new Rect(position.x, currentY, position.width, valueHeight);
                    
                    // Vẽ trường constantValue. EditorGUI.PropertyField đủ thông minh để vẽ control phù hợp
                    // cho hầu hết các kiểu dữ liệu (int, float, string, Vector3, Color, và các struct/class [Serializable])
                    EditorGUI.PropertyField(valueRect, constantValueProp, new GUIContent("Value"), true);
                }
                else
                {
                    // Trường hợp hiếm gặp: không tìm thấy constantValue
                    Rect errorRect = new Rect(position.x, currentY, position.width, lineHeight);
                    EditorGUI.LabelField(errorRect, "Error: 'constantValue' not found.");
                }
            }
            else
            {
                // -- Chế độ giá trị động --
                // Vẽ trường để kéo thả Component nguồn
                contentRect.y = currentY;
                EditorGUI.PropertyField(contentRect, sourceComponentProp, new GUIContent("Source Component"));
                currentY += lineHeight + spacing;
                
                // Vẽ trường để nhập tên của Field/Property nguồn
                contentRect.y = currentY;
                EditorGUI.PropertyField(contentRect, sourceFieldNameProp, new GUIContent("Source Field/Property"));
            }
            
            // Kết thúc khối thụt vào
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    /// <summary>
    /// Hàm này được gọi để tính toán chiều cao tổng cộng cần thiết để vẽ property.
    /// Nó rất quan trọng để đảm bảo không có sự chồng chéo giao diện.
    /// </summary>
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Nếu property bị thu gọn, chỉ cần chiều cao của một dòng cho foldout và label
        if (!property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        // Nếu được mở rộng, tính toán chiều cao chi tiết
        float totalHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Dòng Foldout + Label
        totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;      // Dòng "Use Constant" Toggle

        var useConstantProp = property.FindPropertyRelative("useConstant");

        // === Kiểm tra an toàn ===
        if (useConstantProp == null)
        {
            return totalHeight; // Trả về chiều cao cơ bản nếu có lỗi
        }

        if (useConstantProp.boolValue)
        {
            // Cộng thêm chiều cao của trường constantValue
            var constantValueProp = property.FindPropertyRelative("constantValue");
            if (constantValueProp != null)
            {
                // Sử dụng GetPropertyHeight để tính toán chính xác, kể cả với các kiểu phức tạp như struct
                totalHeight += EditorGUI.GetPropertyHeight(constantValueProp, true) + EditorGUIUtility.standardVerticalSpacing;
            }
        }
        else
        {
            // Cộng thêm chiều cao của 2 trường source (mỗi trường 1 dòng)
            totalHeight += (EditorGUIUtility.singleLineHeight * 2) + (EditorGUIUtility.standardVerticalSpacing * 2);
        }

        return totalHeight;
    }
}