import vue from 'eslint-plugin-vue';
import tseslint from 'typescript-eslint';

const vueFileConfig = vue.configs['flat/recommended'].find((config) =>
  config.files?.includes('**/*.vue'),
);

export default tseslint.config(
  {
    ignores: ['dist/**', 'coverage/**', 'node_modules/**'],
  },
  ...vue.configs['flat/recommended'],
  ...tseslint.configs.recommended,
  {
    files: ['**/*.{js,jsx,ts,tsx}'],
    languageOptions: {
      parserOptions: {
        ecmaVersion: 'latest',
        sourceType: 'module',
        parser: tseslint.parser,
      },
    },
  },
  {
    files: ['**/*.vue'],
    languageOptions: {
      parser: vueFileConfig?.languageOptions?.parser,
      parserOptions: {
        ecmaVersion: 'latest',
        sourceType: 'module',
        parser: tseslint.parser,
      },
    },
  },
);
