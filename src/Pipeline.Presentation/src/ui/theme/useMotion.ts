// Rules: L-01 UI Design System, N-06 Compositor-thread motion with reduced-motion guard
import { useState, useEffect, useRef, useCallback } from 'react';
import { Animated, AccessibilityInfo } from 'react-native';

const IS_TEST_ENV = typeof process !== 'undefined' && process.env['NODE_ENV'] === 'test';

export function useReducedMotion(): boolean {
  const [reducedMotion, setReducedMotion] = useState<boolean>(IS_TEST_ENV);

  useEffect(() => {
    if (IS_TEST_ENV) {
      return;
    }

    let isMounted = true;
    AccessibilityInfo.isReduceMotionEnabled?.()
      ?.then((enabled) => {
        if (isMounted && typeof enabled === 'boolean') {
          setReducedMotion(enabled);
        }
      })
      .catch(() => {});

    const subscription = AccessibilityInfo.addEventListener?.(
      'reduceMotionChanged',
      (enabled: boolean) => {
        if (isMounted) {
          setReducedMotion(Boolean(enabled));
        }
      }
    );

    return () => {
      isMounted = false;
      subscription?.remove?.();
    };
  }, []);

  return reducedMotion;
}

export function usePressScaleAnimation(disabled = false) {
  const reducedMotion = useReducedMotion();
  const scaleAnim = useRef(new Animated.Value(1)).current;
  const opacityAnim = useRef(new Animated.Value(1)).current;

  const onPressIn = useCallback(() => {
    if (disabled || reducedMotion) {
      scaleAnim.setValue(1);
      opacityAnim.setValue(1);
      return;
    }
    Animated.parallel([
      Animated.spring(scaleAnim, {
        toValue: 0.96,
        speed: 36,
        bounciness: 6,
        useNativeDriver: true,
      }),
      Animated.timing(opacityAnim, {
        toValue: 0.9,
        duration: 90,
        useNativeDriver: true,
      }),
    ]).start();
  }, [disabled, opacityAnim, reducedMotion, scaleAnim]);

  const onPressOut = useCallback(() => {
    if (disabled || reducedMotion) {
      scaleAnim.setValue(1);
      opacityAnim.setValue(1);
      return;
    }
    Animated.parallel([
      Animated.spring(scaleAnim, {
        toValue: 1,
        speed: 28,
        bounciness: 8,
        useNativeDriver: true,
      }),
      Animated.timing(opacityAnim, {
        toValue: 1,
        duration: 120,
        useNativeDriver: true,
      }),
    ]).start();
  }, [disabled, opacityAnim, reducedMotion, scaleAnim]);

  return {
    animatedStyle: {
      opacity: opacityAnim,
      transform: [{ scale: scaleAnim }],
    },
    onPressIn,
    onPressOut,
    reducedMotion,
  };
}

