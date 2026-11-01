// Те же пределы, что на сервере (Comments.Api/Domain/CommentLimits.cs).
export const Limits = {
  userNameMax: 50,
  emailMax: 254,
  homePageMax: 2048,
  textMax: 4000,
  captchaMax: 16,
} as const
