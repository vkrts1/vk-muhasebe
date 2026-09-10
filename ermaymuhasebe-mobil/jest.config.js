/** @type {import('jest').Config} */
module.exports = {
  preset: "jest-expo",
  testEnvironment: "node",
  testMatch: ["**/__tests__/integration/**/*.test.[jt]s?(x)"],
  transformIgnorePatterns: [
    "node_modules/(?!((jest-)?react-native|@react-native(-community)?)|expo(nent)?|@expo(nent)?/.*|@expo-google-fonts/.*|react-navigation|@react-navigation/.*|@unimodules/.*|unimodules|sentry-expo|native-base|react-native-svg)"
  ]
};
