// Rules: L-01 Pure UI layer, N-06, N-07
import React from 'react';
import { View, Text, TextInput } from 'react-native';

export interface FormFieldProps {
  readonly label: string;
  readonly value: string;
  readonly onChangeText: (value: string) => void;
  readonly placeholder?: string;
  readonly helperText?: string;
  readonly errorMessage?: string;
  readonly multiline?: boolean;
  readonly editable?: boolean;
  readonly keyboardType?: 'default' | 'email-address' | 'numeric' | 'decimal-pad';
}

export function FormField({
  label,
  value,
  onChangeText,
  placeholder,
  helperText,
  errorMessage,
  multiline = false,
  editable = true,
  keyboardType = 'default',
}: FormFieldProps) {
  const hasError = Boolean(errorMessage);
  const borderClass = hasError
    ? 'border-status-dangerBorder bg-status-dangerBg'
    : editable
    ? 'border-canvas-200 dark:border-canvas-700 bg-canvas-white dark:bg-canvas-900 focus:border-accent-500'
    : 'border-canvas-200 dark:border-canvas-800 bg-canvas-100 dark:bg-canvas-800 opacity-70';

  const heightClass = multiline ? 'min-h-[96px] py-3' : 'min-h-[44px] py-2';

  return (
    <View className="mb-4">
      <Text className="text-sm font-medium text-canvas-900 dark:text-canvas-50 mb-1.5">
        {label}
      </Text>
      <TextInput
        accessibilityLabel={label}
        accessibilityHint={helperText}
        value={value}
        onChangeText={onChangeText}
        placeholder={placeholder}
        editable={editable}
        multiline={multiline}
        keyboardType={keyboardType}
        textAlignVertical={multiline ? 'top' : 'center'}
        className={`${borderClass} ${heightClass} px-3.5 rounded-md border text-sm text-canvas-900 dark:text-canvas-50`}
      />
      {hasError ? (
        <Text
          accessibilityLiveRegion="polite"
          className="text-xs font-medium text-status-dangerText mt-1.5"
        >
          × {errorMessage}
        </Text>
      ) : helperText ? (
        <Text className="text-xs text-canvas-500 dark:text-canvas-400 mt-1.5">
          {helperText}
        </Text>
      ) : null}
    </View>
  );
}

