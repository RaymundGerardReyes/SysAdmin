// Rules: L-01 Pure UI layer, N-06, N-07
import React from 'react';
import { View, Text, TextInput, Pressable } from 'react-native';

export interface FormFieldProps {
  readonly label: string;
  readonly value: string;
  readonly onChangeText: (value: string) => void;
  readonly placeholder?: string | undefined;
  readonly helperText?: string | undefined;
  readonly errorMessage?: string | undefined;
  readonly multiline?: boolean | undefined;
  readonly editable?: boolean | undefined;
  readonly keyboardType?: 'default' | 'email-address' | 'numeric' | 'decimal-pad' | undefined;
  readonly required?: boolean | undefined;
  readonly onClear?: (() => void) | undefined;
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
  required = false,
  onClear,
}: FormFieldProps) {
  const hasError = Boolean(errorMessage);
  const borderClass = hasError
    ? 'border-status-dangerBorder bg-status-dangerBg'
    : editable
    ? 'border-canvas-200 dark:border-canvas-700 bg-canvas-white dark:bg-canvas-900 focus:border-accent-500'
    : 'border-canvas-200 dark:border-canvas-800 bg-canvas-100 dark:bg-canvas-800 opacity-70';

  const heightClass = multiline ? 'min-h-[96px] py-3' : 'min-h-[44px] py-2';
  const showClear = Boolean(onClear && value.length > 0 && editable);

  return (
    <View className="mb-4">
      <View className="flex-row items-center justify-between mb-1.5">
        <Text className="text-sm font-medium text-canvas-900 dark:text-canvas-50">
          {label}
          {required ? <Text className="text-status-dangerText font-bold"> *</Text> : null}
        </Text>
        {multiline && showClear && onClear ? (
          <Pressable
            accessibilityRole="button"
            accessibilityLabel={`Clear ${label}`}
            onPress={onClear}
            className="px-2 py-0.5 rounded-sm active:bg-canvas-100 dark:active:bg-canvas-800"
          >
            <Text className="text-xs text-canvas-500 dark:text-canvas-400 font-medium">Clear</Text>
          </Pressable>
        ) : null}
      </View>

      <View className="relative justify-center">
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
          className={`${borderClass} ${heightClass} ${
            showClear && !multiline ? 'pr-9' : ''
          } px-3.5 rounded-md border text-sm text-canvas-900 dark:text-canvas-50`}
        />
        {showClear && !multiline && onClear ? (
          <Pressable
            accessibilityRole="button"
            accessibilityLabel={`Clear ${label}`}
            onPress={onClear}
            className="absolute right-2 p-1.5 min-w-[32px] min-h-[32px] items-center justify-center rounded-full active:bg-canvas-200 dark:active:bg-canvas-700"
          >
            <Text className="text-canvas-400 dark:text-canvas-500 font-bold text-xs">✕</Text>
          </Pressable>
        ) : null}
      </View>

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
