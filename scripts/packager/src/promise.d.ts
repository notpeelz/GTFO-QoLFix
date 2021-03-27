// https://stackoverflow.com/a/49889856/1581233
type ThenArg<T> = T extends PromiseLike<infer U> ? U : T;
