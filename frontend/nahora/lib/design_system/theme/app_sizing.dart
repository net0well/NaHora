/// Minimum hit target. Desktop pointers are precise, but the same screens run
/// on touch devices later and 44 logical pixels is the accessible floor.
abstract final class AppSizing {
  static const double minTapTarget = 44;
  static const double buttonHeight = 48;
  static const double inputHeight = 48;
}
